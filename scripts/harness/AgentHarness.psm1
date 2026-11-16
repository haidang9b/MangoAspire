#Requires -Version 5.1
<#
.SYNOPSIS
    Agent-harness tooling for MangoAspire.
.DESCRIPTION
    .claude/ is canonical (rules, skills, agents). This module mirrors the parts Copilot cannot read
    from .claude/ into .github/ (rules -> .github/instructions, agents -> .github/agents) and detects
    drift between the two. Must run unchanged on Windows PowerShell 5.1 and PowerShell 7.
#>

$script:Banner = '<!-- GENERATED from .claude/ by scripts/sync-agent-harness.ps1 - do not edit directly -->'
$script:Utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$script:MirrorDirs = @('instructions', 'agents')

# --- private helpers ---------------------------------------------------------------------------

function Remove-Quotes([string]$Value) {
    if ($null -eq $Value) { return $Value }
    $v = $Value.Trim()
    if ($v.Length -ge 2 -and (($v[0] -eq '"' -and $v[-1] -eq '"') -or ($v[0] -eq "'" -and $v[-1] -eq "'"))) {
        return $v.Substring(1, $v.Length - 2)
    }
    return $v
}

function Write-Utf8NoBom([string]$Path, [string]$Content) {
    $dir = Split-Path -Parent $Path
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    [System.IO.File]::WriteAllText($Path, $Content, $script:Utf8NoBom)
}

function Read-Normalized([string]$Path) {
    return ([System.IO.File]::ReadAllText($Path) -replace "`r`n", "`n")
}

function Get-RelativePath([string]$Root, [string]$FullName) {
    $rootFull = (Get-Item -LiteralPath $Root).FullName.TrimEnd('\', '/')
    return $FullName.Substring($rootFull.Length).TrimStart('\', '/').Replace('\', '/')
}

# Returns the key line plus its indented continuation lines for a top-level frontmatter key.
function Get-FrontmatterBlock([string[]]$Front, [string]$Key) {
    $block = @()
    for ($i = 0; $i -lt $Front.Count; $i++) {
        if ($Front[$i] -match ('^' + [regex]::Escape($Key) + '\s*:')) {
            $block += $Front[$i]
            for ($j = $i + 1; $j -lt $Front.Count; $j++) {
                if ($Front[$j] -match '^\s+\S') { $block += $Front[$j] } else { break }
            }
            break
        }
    }
    return $block
}

# Map of "<sub>/<rel>" -> full path for the mirror subdirectories under $Root.
function Get-MirrorFiles([string]$Root) {
    $map = @{}
    foreach ($sub in $script:MirrorDirs) {
        $base = Join-Path $Root $sub
        if (-not (Test-Path $base)) { continue }
        foreach ($f in Get-ChildItem -LiteralPath $base -Recurse -File) {
            $map["$sub/" + (Get-RelativePath $base $f.FullName)] = $f.FullName
        }
    }
    return $map
}

# --- frontmatter -------------------------------------------------------------------------------

function Split-Frontmatter {
    [CmdletBinding()]
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Text)
    $lines = $Text -split "`r?`n"
    if ($lines.Count -gt 0 -and $lines[0].Trim() -eq '---') {
        for ($i = 1; $i -lt $lines.Count; $i++) {
            if ($lines[$i].Trim() -eq '---') {
                $front = if ($i -gt 1) { @($lines[1..($i - 1)]) } else { @() }
                $body = if ($i + 1 -le $lines.Count - 1) { $lines[($i + 1)..($lines.Count - 1)] -join "`n" } else { '' }
                return @{ Front = $front; Body = $body }
            }
        }
    }
    return @{ Front = @(); Body = ($lines -join "`n") }
}

function Get-FrontmatterValue {
    [CmdletBinding()]
    param([AllowEmptyCollection()][string[]]$Front = @(), [Parameter(Mandatory)][string]$Key)
    foreach ($line in $Front) {
        if ($line -match ('^' + [regex]::Escape($Key) + '\s*:\s*(.*)$')) { return (Remove-Quotes $Matches[1]) }
    }
    return $null
}

function Get-FrontmatterList {
    [CmdletBinding()]
    param([AllowEmptyCollection()][string[]]$Front = @(), [Parameter(Mandatory)][string]$Key)
    $result = New-Object System.Collections.Generic.List[string]
    for ($i = 0; $i -lt $Front.Count; $i++) {
        if ($Front[$i] -notmatch ('^' + [regex]::Escape($Key) + '\s*:\s*(.*)$')) { continue }
        $inline = $Matches[1].Trim()
        if ($inline -eq '') {
            for ($j = $i + 1; $j -lt $Front.Count; $j++) {
                if ($Front[$j] -match '^\s*-\s*(.*)$') { $result.Add((Remove-Quotes $Matches[1])) }
                elseif ($Front[$j] -match '^\s*$') { continue }
                else { break }
            }
        }
        else {
            if ($inline.StartsWith('[') -and $inline.EndsWith(']')) { $inline = $inline.Substring(1, $inline.Length - 2) }
            else { $inline = Remove-Quotes $inline }
            foreach ($part in $inline.Split(',')) {
                $p = Remove-Quotes $part
                if ($p) { $result.Add($p) }
            }
        }
        break
    }
    return $result.ToArray()
}

# --- Copilot mirror ----------------------------------------------------------------------------

function ConvertTo-CopilotInstruction {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Text)
    $parts = Split-Frontmatter -Text $Text
    $globs = @(Get-FrontmatterList -Front $parts.Front -Key 'paths')
    if ($globs.Count -eq 0) { throw "Rule has no 'paths' frontmatter, so it cannot be mapped to a Copilot applyTo." }
    $body = $parts.Body.TrimStart("`n").TrimEnd()
    return "---`napplyTo: `"$($globs -join ',')`"`n---`n`n$($script:Banner)`n`n$body`n"
}

function ConvertTo-CopilotAgent {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Text)
    $parts = Split-Frontmatter -Text $Text
    $front = @(Get-FrontmatterBlock $parts.Front 'name') + @(Get-FrontmatterBlock $parts.Front 'description')
    if (-not (Get-FrontmatterValue -Front $parts.Front -Key 'name')) { throw "Agent has no 'name' frontmatter." }
    $body = $parts.Body.TrimStart("`n").TrimEnd()
    return "---`n$($front -join "`n")`n---`n`n$($script:Banner)`n`n$body`n"
}

function Invoke-HarnessGenerate {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepoRoot, [Parameter(Mandatory)][string]$OutRoot)
    $rulesDir = Join-Path $RepoRoot '.claude/rules'
    if (Test-Path $rulesDir) {
        foreach ($f in Get-ChildItem -LiteralPath $rulesDir -Recurse -File -Filter '*.md') {
            $rel = (Get-RelativePath $rulesDir $f.FullName) -replace '\.md$', '.instructions.md'
            $dest = Join-Path (Join-Path $OutRoot 'instructions') $rel
            Write-Utf8NoBom $dest (ConvertTo-CopilotInstruction -Text ([System.IO.File]::ReadAllText($f.FullName)))
        }
    }
    $agentsDir = Join-Path $RepoRoot '.claude/agents'
    if (Test-Path $agentsDir) {
        foreach ($f in Get-ChildItem -LiteralPath $agentsDir -File -Filter '*.md') {
            $dest = Join-Path (Join-Path $OutRoot 'agents') ($f.BaseName + '.agent.md')
            Write-Utf8NoBom $dest (ConvertTo-CopilotAgent -Text ([System.IO.File]::ReadAllText($f.FullName)))
        }
    }
}

function Invoke-HarnessSync {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepoRoot)
    $github = Join-Path $RepoRoot '.github'
    foreach ($sub in $script:MirrorDirs) {
        $p = Join-Path $github $sub
        if (Test-Path $p) { Remove-Item -LiteralPath $p -Recurse -Force }
    }
    Invoke-HarnessGenerate -RepoRoot $RepoRoot -OutRoot $github
}

function Get-HarnessDrift {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepoRoot)
    $tmp = Join-Path ([System.IO.Path]::GetTempPath()) ('mango-harness-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $tmp -Force | Out-Null
    try {
        Invoke-HarnessGenerate -RepoRoot $RepoRoot -OutRoot $tmp
        $expected = Get-MirrorFiles $tmp
        $actual = Get-MirrorFiles (Join-Path $RepoRoot '.github')
        $out = New-Object System.Collections.Generic.List[string]
        foreach ($rel in $expected.Keys) {
            if (-not $actual.ContainsKey($rel)) { $out.Add("missing: $rel"); continue }
            if ((Read-Normalized $expected[$rel]) -cne (Read-Normalized $actual[$rel])) { $out.Add("changed: $rel") }
        }
        foreach ($rel in $actual.Keys) {
            if (-not $expected.ContainsKey($rel)) { $out.Add("extra: $rel") }
        }
        return ($out | Sort-Object)
    }
    finally {
        Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Export-ModuleMember -Function Split-Frontmatter, Get-FrontmatterValue, Get-FrontmatterList,
    ConvertTo-CopilotInstruction, ConvertTo-CopilotAgent, Invoke-HarnessGenerate, Invoke-HarnessSync,
    Get-HarnessDrift
