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

# --- lint: shared ------------------------------------------------------------------------------

$script:Budget = @{
    AgentsMd = 4096; ClaudeMd = 1024; AlwaysOn = 5120; Rule = 4096
    SkillMd = 6144; Description = 300; ModelSkills = 6; AgentBody = 3072
}
$script:LinkPrefixes = @('.agent/', '.claude/', '.github/', 'src/', 'tests/', 'docs/', 'scripts/', 'specs/')
# Case-sensitive substrings that mean a harness file has drifted from the repo. Conceptual terms
# such as "Repository pattern" are deliberately absent: guardrails legitimately name them.
$script:StaleTerms = @(
    'npm run', 'npm install', 'src/backend', 'Vitest', 'React Query',
    'docs/tracking', 'docs/memory/', 'docs/plans/', 'docs/archive/',
    '.agent/skills', '.agent/agents', '.agent/workflows', '.agent/rules', '.claude/commands',
    'CODING_CONVENTIONS', 'API_PROJECT_STRUCTURE', 'MUI', 'DataGrid',
    'onboard-analyzer', 'manage-documentation', 'analyze-po-requirement', 'analyze-codebase'
)
$script:LintChecks = [ordered]@{}

function New-Finding([string]$Category, [string]$File, [string]$Message) {
    [pscustomobject]@{ Category = $Category; File = $File; Message = $Message }
}

function Get-RepoRelative([string]$RepoRoot, [string]$FullName) { Get-RelativePath $RepoRoot $FullName }

function Get-MarkdownUnder([string]$Dir, [switch]$Recurse) {
    if (-not (Test-Path -LiteralPath $Dir)) { return @() }
    return @(Get-ChildItem -LiteralPath $Dir -File -Filter '*.md' -Recurse:$Recurse | ForEach-Object { $_.FullName })
}

# Harness set: AGENTS.md, CLAUDE.md, .claude/**/*.md, .agent/README.md, .agent/tools/*.md, .agent/memory/**/*.md
function Get-HarnessFiles([string]$RepoRoot) {
    $files = @()
    foreach ($rel in 'AGENTS.md', 'CLAUDE.md', '.agent/README.md') {
        $p = Join-Path $RepoRoot $rel
        if (Test-Path -LiteralPath $p -PathType Leaf) { $files += (Get-Item -LiteralPath $p).FullName }
    }
    $files += Get-MarkdownUnder (Join-Path $RepoRoot '.claude') -Recurse
    $files += Get-MarkdownUnder (Join-Path $RepoRoot '.agent/tools')
    $files += Get-MarkdownUnder (Join-Path $RepoRoot '.agent/memory') -Recurse
    return $files
}

function Get-MarkdownLinkFiles([string]$RepoRoot) {
    return @(Get-HarnessFiles $RepoRoot) + @(Get-MarkdownUnder (Join-Path $RepoRoot 'specs') -Recurse)
}

# Lines outside fenced code blocks. A fence closes only on the marker (``` or ~~~) that opened it.
function Get-UnfencedLines([string]$Text) {
    $out = New-Object System.Collections.Generic.List[object]
    $fence = $null
    $n = 0
    foreach ($line in ($Text -split "`r?`n")) {
        $n++
        $t = $line.TrimStart()
        $marker = $null
        if ($t.StartsWith('```')) { $marker = '```' } elseif ($t.StartsWith('~~~')) { $marker = '~~~' }
        if ($null -ne $fence) {
            if ($marker -eq $fence) { $fence = $null }
            continue
        }
        if ($marker) { $fence = $marker; continue }
        $out.Add([pscustomobject]@{ Line = $line; Number = $n })
    }
    return $out
}

function Get-SkillInfo([string]$RepoRoot) {
    $dir = Join-Path $RepoRoot '.claude/skills'
    if (-not (Test-Path -LiteralPath $dir)) { return @() }
    foreach ($d in Get-ChildItem -LiteralPath $dir -Directory) {
        $md = Join-Path $d.FullName 'SKILL.md'
        if (-not (Test-Path -LiteralPath $md)) {
            [pscustomobject]@{ Folder = $d.Name; Path = $null; Name = $null; Description = $null; UserOnly = $false; Bytes = 0 }
            continue
        }
        $front = (Split-Frontmatter -Text ([System.IO.File]::ReadAllText($md))).Front
        [pscustomobject]@{
            Folder      = $d.Name
            Path        = (Get-Item -LiteralPath $md).FullName
            Name        = Get-FrontmatterValue -Front $front -Key 'name'
            Description = Get-FrontmatterValue -Front $front -Key 'description'
            UserOnly    = ((Get-FrontmatterValue -Front $front -Key 'disable-model-invocation') -eq 'true')
            Bytes       = (Get-Item -LiteralPath $md).Length
        }
    }
}

function Add-AlwaysOnFile([string]$Path, [int]$Depth, [hashtable]$Seen) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return }
    $full = (Get-Item -LiteralPath $Path).FullName
    if ($Seen.ContainsKey($full)) { return }
    $Seen[$full] = (Get-Item -LiteralPath $full).Length
    if ($Depth -ge 4) { return }
    $baseDir = Split-Path -Parent $full
    foreach ($l in Get-UnfencedLines ([System.IO.File]::ReadAllText($full))) {
        $line = $l.Line -replace '`[^`]*`', ''
        foreach ($m in [regex]::Matches($line, '(?<![\w@])@([^\s`@]+)')) {
            $target = $m.Groups[1].Value.TrimEnd('.', ',', ';', ':', ')')
            Add-AlwaysOnFile (Join-Path $baseDir $target) ($Depth + 1) $Seen
        }
    }
}

function Get-AlwaysOnBytes([string]$RepoRoot) {
    $seen = @{}
    Add-AlwaysOnFile (Join-Path $RepoRoot 'CLAUDE.md') 0 $seen
    $total = 0
    foreach ($v in $seen.Values) { $total += $v }
    foreach ($r in Get-MarkdownUnder (Join-Path $RepoRoot '.claude/rules') -Recurse) {
        $front = (Split-Frontmatter -Text ([System.IO.File]::ReadAllText($r))).Front
        if (@(Get-FrontmatterList -Front $front -Key 'paths').Count -eq 0) { $total += (Get-Item -LiteralPath $r).Length }
    }
    return $total
}

# --- lint: categories --------------------------------------------------------------------------

function Test-Budget([string]$RepoRoot) {
    foreach ($pair in @(@('AGENTS.md', $script:Budget.AgentsMd), @('CLAUDE.md', $script:Budget.ClaudeMd))) {
        $p = Join-Path $RepoRoot $pair[0]
        if ((Test-Path -LiteralPath $p) -and (Get-Item -LiteralPath $p).Length -gt $pair[1]) {
            New-Finding 'Budget' $pair[0] ("{0} is {1} bytes (budget {2})" -f $pair[0], (Get-Item -LiteralPath $p).Length, $pair[1])
        }
    }
    $always = Get-AlwaysOnBytes $RepoRoot
    if ($always -gt $script:Budget.AlwaysOn) {
        New-Finding 'Budget' 'CLAUDE.md' ("always-on context is {0} bytes (budget {1}): CLAUDE.md + its @-imports + rules without paths" -f $always, $script:Budget.AlwaysOn)
    }
}

function Test-Rules([string]$RepoRoot) {
    foreach ($r in Get-MarkdownUnder (Join-Path $RepoRoot '.claude/rules') -Recurse) {
        $rel = Get-RepoRelative $RepoRoot $r
        $front = (Split-Frontmatter -Text ([System.IO.File]::ReadAllText($r))).Front
        $globs = @(Get-FrontmatterList -Front $front -Key 'paths')
        if ($globs.Count -eq 0) { New-Finding 'Rules' $rel 'rule has no paths, so Claude Code loads it in every session' }
        foreach ($g in $globs) {
            if ($g -match '\s') { New-Finding 'Rules' $rel "paths entry '$g' is not a glob" }
        }
        $size = (Get-Item -LiteralPath $r).Length
        if ($size -gt $script:Budget.Rule) { New-Finding 'Rules' $rel ("rule is {0} bytes (budget {1})" -f $size, $script:Budget.Rule) }
    }
}

function Test-Skills([string]$RepoRoot) {
    $skills = @(Get-SkillInfo $RepoRoot)
    foreach ($s in $skills) {
        $rel = ".claude/skills/$($s.Folder)/SKILL.md"
        if (-not $s.Path) { New-Finding 'Skills' ".claude/skills/$($s.Folder)" 'skill folder has no SKILL.md'; continue }
        if ($s.Name -cne $s.Folder) { New-Finding 'Skills' $rel "name '$($s.Name)' does not match folder '$($s.Folder)'" }
        if (-not $s.Description) { New-Finding 'Skills' $rel 'description is missing' }
        else {
            if (-not $s.Description.StartsWith('Use when', [System.StringComparison]::Ordinal)) {
                New-Finding 'Skills' $rel "description must start with 'Use when' (trigger-only)"
            }
            if ($s.Description.Length -gt $script:Budget.Description) {
                New-Finding 'Skills' $rel ("description is {0} chars (budget {1})" -f $s.Description.Length, $script:Budget.Description)
            }
        }
        if ($s.Bytes -gt $script:Budget.SkillMd) { New-Finding 'Skills' $rel ("SKILL.md is {0} bytes (budget {1})" -f $s.Bytes, $script:Budget.SkillMd) }
    }
    $model = @($skills | Where-Object { $_.Path -and -not $_.UserOnly })
    if ($model.Count -gt $script:Budget.ModelSkills) {
        New-Finding 'Skills' '.claude/skills' ("{0} model-invocable skills (budget {1}); set disable-model-invocation: true on human-run workflows" -f $model.Count, $script:Budget.ModelSkills)
    }
    # User-only skills cannot be invoked by the model, so other files must reference them by path.
    $refFiles = @($skills | Where-Object { $_.Path } | ForEach-Object { $_.Path })
    $refFiles += Get-MarkdownUnder (Join-Path $RepoRoot '.claude/agents')
    foreach ($rel in 'AGENTS.md', 'CLAUDE.md') {
        $p = Join-Path $RepoRoot $rel
        if (Test-Path -LiteralPath $p) { $refFiles += (Get-Item -LiteralPath $p).FullName }
    }
    foreach ($u in @($skills | Where-Object { $_.Path -and $_.UserOnly })) {
        $token = '`' + $u.Folder + '`'
        foreach ($f in $refFiles) {
            if ($f -eq $u.Path) { continue }
            if ([System.IO.File]::ReadAllText($f).Contains($token)) {
                New-Finding 'Skills' (Get-RepoRelative $RepoRoot $f) ("names user-only skill {0} by bare name; reference .claude/skills/{1}/SKILL.md instead" -f $token, $u.Folder)
            }
        }
    }
}

function Test-Agents([string]$RepoRoot) {
    foreach ($a in Get-MarkdownUnder (Join-Path $RepoRoot '.claude/agents')) {
        $rel = Get-RepoRelative $RepoRoot $a
        $parts = Split-Frontmatter -Text ([System.IO.File]::ReadAllText($a))
        $name = Get-FrontmatterValue -Front $parts.Front -Key 'name'
        $desc = Get-FrontmatterValue -Front $parts.Front -Key 'description'
        $base = [System.IO.Path]::GetFileNameWithoutExtension($a)
        if ($name -cne $base) { New-Finding 'Agents' $rel "name '$name' does not match file '$base'" }
        if (-not $desc) { New-Finding 'Agents' $rel 'description is missing' }
        elseif ($desc.Length -gt $script:Budget.Description) { New-Finding 'Agents' $rel ("description is {0} chars (budget {1})" -f $desc.Length, $script:Budget.Description) }
        $bodyBytes = $script:Utf8NoBom.GetByteCount($parts.Body)
        if ($bodyBytes -gt $script:Budget.AgentBody) { New-Finding 'Agents' $rel ("body is {0} bytes (budget {1})" -f $bodyBytes, $script:Budget.AgentBody) }
    }
}

function Test-Links([string]$RepoRoot) {
    # (a) backticked repo paths in the harness set
    foreach ($f in Get-HarnessFiles $RepoRoot) {
        $rel = Get-RepoRelative $RepoRoot $f
        foreach ($l in Get-UnfencedLines ([System.IO.File]::ReadAllText($f))) {
            foreach ($m in [regex]::Matches($l.Line, '`([^`]+)`')) {
                $t = $m.Groups[1].Value
                $hasPrefix = $false
                foreach ($p in $script:LinkPrefixes) { if ($t.StartsWith($p, [System.StringComparison]::Ordinal)) { $hasPrefix = $true; break } }
                if (-not $hasPrefix -or $t -match '[<>*{}$|\s]') { continue }
                $t = ($t -replace ':\d+$', '').TrimEnd('/')
                if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot $t))) {
                    New-Finding 'Links' $rel ("backticked path ``{0}`` does not exist (line {1})" -f $t, $l.Number)
                }
            }
        }
    }
    # (b) relative markdown links in the harness set and specs/ (ported from agentic-devkit check_kit.py)
    foreach ($f in Get-MarkdownLinkFiles $RepoRoot) {
        $rel = Get-RepoRelative $RepoRoot $f
        $dir = Split-Path -Parent $f
        foreach ($l in Get-UnfencedLines ([System.IO.File]::ReadAllText($f))) {
            $line = $l.Line -replace '`[^`]*`', ''
            foreach ($m in [regex]::Matches($line, '!?\[[^\]]*\]\(([^)]*)\)')) {
                $raw = ($m.Groups[1].Value.Trim() -replace '\s+"[^"]*"$', '')
                if (-not $raw -or $raw -match '^(https?://|mailto:|#)' -or $raw -match '[<>{}$*]') { continue }
                $target = [System.Uri]::UnescapeDataString($raw.Split('#')[0])
                if (-not $target) { continue }
                $resolved = if ($target.StartsWith('/')) { Join-Path $RepoRoot $target.TrimStart('/') } else { Join-Path $dir $target }
                if (-not (Test-Path -LiteralPath $resolved)) {
                    New-Finding 'Links' $rel ("broken link -> {0} (line {1})" -f $raw, $l.Number)
                }
            }
        }
    }
}

function Test-Staleness([string]$RepoRoot) {
    foreach ($f in Get-HarnessFiles $RepoRoot) {
        $text = [System.IO.File]::ReadAllText($f)
        foreach ($term in $script:StaleTerms) {
            if ($text.Contains($term)) { New-Finding 'Staleness' (Get-RepoRelative $RepoRoot $f) "stale term '$term'" }
        }
    }
}

function Test-Drift([string]$RepoRoot) {
    foreach ($d in @(Get-HarnessDrift -RepoRoot $RepoRoot)) {
        $rel = $d.Substring($d.IndexOf(': ') + 2)
        New-Finding 'Drift' ".github/$rel" "$d (run scripts/sync-agent-harness.ps1)"
    }
}

$script:LintChecks['Budget'] = { param($r) Test-Budget $r }
$script:LintChecks['Rules'] = { param($r) Test-Rules $r }
$script:LintChecks['Skills'] = { param($r) Test-Skills $r }
$script:LintChecks['Agents'] = { param($r) Test-Agents $r }
$script:LintChecks['Links'] = { param($r) Test-Links $r }
$script:LintChecks['Staleness'] = { param($r) Test-Staleness $r }
$script:LintChecks['Drift'] = { param($r) Test-Drift $r }

# --- lint: public ------------------------------------------------------------------------------

function Invoke-HarnessLint {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepoRoot, [string[]]$Category)
    $root = (Get-Item -LiteralPath $RepoRoot).FullName
    $cats = if ($Category) { @($Category | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ }) } else { @($script:LintChecks.Keys) }
    foreach ($c in $cats) {
        if (-not $script:LintChecks.Contains($c)) {
            throw ("Unknown lint category '{0}'. Known: {1}" -f $c, (@($script:LintChecks.Keys) -join ', '))
        }
        & $script:LintChecks[$c] $root
    }
}

function Get-HarnessContextReport {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepoRoot)
    $root = (Get-Item -LiteralPath $RepoRoot).FullName
    $skills = @(Get-SkillInfo $root | Where-Object { $_.Path })
    $model = @($skills | Where-Object { -not $_.UserOnly })
    $chars = 0
    foreach ($s in $model) { if ($s.Description) { $chars += $s.Description.Length } }
    [pscustomobject]@{
        AlwaysOnBytes     = Get-AlwaysOnBytes $root
        ModelSkillCount   = $model.Count
        UserSkillCount    = $skills.Count - $model.Count
        SkillListingChars = $chars
    }
}

Export-ModuleMember -Function Split-Frontmatter, Get-FrontmatterValue, Get-FrontmatterList,
    ConvertTo-CopilotInstruction, ConvertTo-CopilotAgent, Invoke-HarnessGenerate, Invoke-HarnessSync,
    Get-HarnessDrift, Invoke-HarnessLint, Get-HarnessContextReport
