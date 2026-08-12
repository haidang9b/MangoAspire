#Requires -Version 5.1
<#
.SYNOPSIS
    Generates Claude Code's native harness (.claude/skills, .claude/agents, .claude/commands)
    from the source-of-truth .agent/ tree.
.DESCRIPTION
    .agent/ is the single source of truth. This script converts it into the layout Claude Code
    discovers:
      .agent/skills/<x>/SKILL.md      -> .claude/skills/<x>/SKILL.md   (frontmatter name := dir name)
      .agent/agents/<x>.agent.md      -> .claude/agents/<x>.md          (non-Claude tools:/argument-hint dropped)
      .agent/workflows/<x>.md         -> .claude/commands/<x>.md        (@-imports the workflow doc)
    Generated files carry a "do not edit" banner. Mechanical path fixups are applied; conceptual
    staleness (npm, Vitest, Clean/Onion Architecture, Repository pattern) is only WARNED about so it
    can be fixed deliberately at the source.
.PARAMETER Check
    Dry-run. Generates into a temp dir, compares against the committed .claude/, and exits non-zero
    if they differ (for pre-commit / CI). No files are written to .claude/.
#>
[CmdletBinding()]
param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$agentRoot = Join-Path $repoRoot '.agent'

$Banner = '<!-- GENERATED from .agent/ by scripts/sync-agent-harness.ps1 - do not edit directly -->'
$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

# Mechanical path fixups that are always safe to apply automatically.
$SafeReplacements = [ordered]@{
    'src/frontend' = 'src/UI/mango-ui'
}
# Terms that indicate conceptual staleness vs AGENTS.md - warned, never auto-edited.
# The docs/* entries catch anything reintroducing the retired memory/ticket locations, which now
# live under .agent/memory and .agent/tickets.
$WarnPatterns = @('npm run', 'npm install', 'src/backend', 'Vitest', 'Clean Architecture', 'Onion', 'Repository pattern', 'React Query',
    'docs/tracking', 'docs/memory/', 'docs/plans/', 'docs/archive/')

function Write-FileNoBom {
    param([string]$Path, [string]$Content)
    $dir = Split-Path -Parent $Path
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    [System.IO.File]::WriteAllText($Path, $Content, $Utf8NoBom)
}

function Apply-Fixups {
    param([string]$Text)
    foreach ($k in $SafeReplacements.Keys) {
        $Text = $Text.Replace($k, $SafeReplacements[$k])
    }
    return $Text
}

# Splits a markdown file into @{ Front = <lines[]>; Body = <string> }. Front excludes the --- fences.
function Split-Frontmatter {
    param([string]$RawText)
    $lines = $RawText -split "`r?`n"
    if ($lines.Count -gt 0 -and $lines[0].Trim() -eq '---') {
        for ($i = 1; $i -lt $lines.Count; $i++) {
            if ($lines[$i].Trim() -eq '---') {
                $front = if ($i -gt 1) { $lines[1..($i - 1)] } else { @() }
                $bodyLines = if ($i + 1 -le $lines.Count - 1) { $lines[($i + 1)..($lines.Count - 1)] } else { @() }
                return @{ Front = $front; Body = ($bodyLines -join "`n") }
            }
        }
    }
    return @{ Front = @(); Body = $RawText }
}

function Generate-Into {
    param([string]$TargetRoot)

    # --- Skills: recursive copy; SKILL.md gets name normalised to dir name + banner + fixups ---
    $skillsSrc = Join-Path $agentRoot 'skills'
    foreach ($skillDir in Get-ChildItem -Path $skillsSrc -Directory) {
        $slug = $skillDir.Name
        foreach ($file in Get-ChildItem -Path $skillDir.FullName -Recurse -File) {
            $rel = $file.FullName.Substring($skillDir.FullName.Length).TrimStart('\', '/')
            $destPath = Join-Path (Join-Path (Join-Path $TargetRoot 'skills') $slug) $rel
            $raw = [System.IO.File]::ReadAllText($file.FullName)
            $raw = Apply-Fixups $raw

            if ($file.Name -eq 'SKILL.md' -and $rel -eq 'SKILL.md') {
                $parts = Split-Frontmatter $raw
                $front = @()
                $hasName = $false
                foreach ($fl in $parts.Front) {
                    if ($fl -match '^\s*name\s*:') { $front += "name: $slug"; $hasName = $true }
                    else { $front += $fl }
                }
                if (-not $hasName) { $front = @("name: $slug") + $front }
                $content = "---`n" + ($front -join "`n") + "`n---`n`n$Banner`n" + $parts.Body.TrimStart("`n")
                Write-FileNoBom -Path $destPath -Content $content
            }
            else {
                Write-FileNoBom -Path $destPath -Content $raw
            }
        }
    }

    # --- Agents: .agent.md -> .md, drop non-Claude tools:/argument-hint, keep name+description ---
    $agentsSrc = Join-Path $agentRoot 'agents'
    foreach ($file in Get-ChildItem -Path $agentsSrc -File -Filter '*.agent.md') {
        $name = $file.Name -replace '\.agent\.md$', ''
        $raw = Apply-Fixups ([System.IO.File]::ReadAllText($file.FullName))
        $parts = Split-Frontmatter $raw

        $front = @()
        $argHint = $null
        $skipList = $false
        foreach ($fl in $parts.Front) {
            if ($fl -match '^\s*tools\s*:') { $skipList = $true; continue }
            if ($fl -match '^\s*argument-hint\s*:\s*(.*)$') { $argHint = $Matches[1].Trim(); continue }
            if ($skipList) {
                # Skip the indented YAML list items belonging to tools:.
                if ($fl -match '^\s*-\s' -or $fl -match '^\s+\S') { continue }
                $skipList = $false
            }
            $front += $fl
        }

        $body = $parts.Body.TrimStart("`n")
        if ($argHint) { $body = "> Usage: $argHint`n`n$body" }
        $content = "---`n" + ($front -join "`n") + "`n---`n`n$Banner`n`n$body"
        $destPath = Join-Path (Join-Path $TargetRoot 'agents') "$name.md"
        Write-FileNoBom -Path $destPath -Content $content
    }

    # --- Commands: one per workflow, @-importing the source workflow doc ---
    $wfSrc = Join-Path $agentRoot 'workflows'
    foreach ($file in Get-ChildItem -Path $wfSrc -File -Filter '*.md') {
        $name = $file.BaseName
        $raw = [System.IO.File]::ReadAllText($file.FullName)
        $parts = Split-Frontmatter $raw
        $desc = "Run the $name workflow."
        foreach ($fl in $parts.Front) {
            if ($fl -match '^\s*description\s*:\s*(.*)$') { $desc = $Matches[1].Trim(); break }
        }
        $rel = ".agent/workflows/$name.md"
        $content = @"
---
description: $desc
---

$Banner

Execute the following workflow end-to-end for this repository. Follow every step and honour the
project conventions in AGENTS.md (Vertical Slice architecture, pnpm, xUnit/Moq/Shouldly).

@$rel
"@
        $destPath = Join-Path (Join-Path $TargetRoot 'commands') "$name.md"
        Write-FileNoBom -Path $destPath -Content $content
    }
}

# Returns a sorted list of "relpath|sha256" for every file under the three generated dirs of $root.
function Get-TreeSignature {
    param([string]$Root)
    $sig = @()
    foreach ($sub in @('skills', 'agents', 'commands')) {
        $base = Join-Path $Root $sub
        if (-not (Test-Path $base)) { continue }
        foreach ($f in Get-ChildItem -Path $base -Recurse -File) {
            $rel = $f.FullName.Substring($Root.Length).TrimStart('\', '/').Replace('\', '/')
            $hash = (Get-FileHash -Path $f.FullName -Algorithm SHA256).Hash
            $sig += "$rel|$hash"
        }
    }
    return ($sig | Sort-Object)
}

if ($Check) {
    $tmp = Join-Path ([System.IO.Path]::GetTempPath()) ("mango-harness-" + [System.Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $tmp -Force | Out-Null
    try {
        Generate-Into -TargetRoot $tmp
        $expected = Get-TreeSignature -Root $tmp
        $actual = Get-TreeSignature -Root (Join-Path $repoRoot '.claude')
        $diff = Compare-Object -ReferenceObject $expected -DifferenceObject $actual
        if ($diff) {
            Write-Warning "'.claude/' is OUT OF SYNC with '.agent/'. Run: pwsh ./scripts/sync-agent-harness.ps1"
            $diff | ForEach-Object {
                $side = if ($_.SideIndicator -eq '<=') { 'missing/changed' } else { 'stale/extra' }
                Write-Host ("  [{0}] {1}" -f $side, $_.InputObject)
            }
            exit 1
        }
        Write-Host ".claude/ is in sync with .agent/." -ForegroundColor Green
        exit 0
    }
    finally {
        Remove-Item -Path $tmp -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# Write mode: regenerate the three generated dirs (leave hand-authored .claude/settings.json alone).
foreach ($sub in @('skills', 'agents', 'commands')) {
    $p = Join-Path (Join-Path $repoRoot '.claude') $sub
    if (Test-Path $p) { Remove-Item -Path $p -Recurse -Force }
}
Generate-Into -TargetRoot (Join-Path $repoRoot '.claude')

# Warn on conceptual staleness that survived into the generated output.
$warnHits = @()
foreach ($sub in @('skills', 'agents', 'commands')) {
    $base = Join-Path (Join-Path $repoRoot '.claude') $sub
    if (-not (Test-Path $base)) { continue }
    foreach ($f in Get-ChildItem -Path $base -Recurse -File -Filter '*.md') {
        $text = [System.IO.File]::ReadAllText($f.FullName)
        foreach ($pat in $WarnPatterns) {
            if ($text -match [Regex]::Escape($pat)) {
                $warnHits += ("  {0}: '{1}'" -f $f.FullName.Substring($repoRoot.Length).TrimStart('\', '/'), $pat)
            }
        }
    }
}

Write-Host "Generated .claude/skills, .claude/agents, .claude/commands from .agent/." -ForegroundColor Green
if ($warnHits.Count -gt 0) {
    Write-Warning "Possible conceptual staleness vs AGENTS.md (fix at the SOURCE in .agent/, then re-sync):"
    $warnHits | Sort-Object -Unique | ForEach-Object { Write-Host $_ -ForegroundColor Yellow }
}
