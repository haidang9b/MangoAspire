#Requires -Version 5.1
<#
.SYNOPSIS
    Skill output evals for MangoAspire: run each eval with and without the skill, grade, benchmark.
.DESCRIPTION
    Adapted from the agentic-devkit skill-creator method (evals.json, grader, benchmark) to a scripted
    runner. Eval definitions live in tests/skill-evals/<skill>/evals.json, outside the skill folder,
    so a with-skill session cannot read the expected answers. Runs on Windows PowerShell 5.1 and
    PowerShell 7.
#>

$script:CheckTypes = @('command', 'file-matches', 'no-edits', 'no-commit', 'response-matches')
$script:RunTools = 'Read,Grep,Glob,Edit,Write,Bash(dotnet build:*),Bash(dotnet test:*),Bash(git diff:*),Bash(git status:*),Bash(git log:*)'
$script:Utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$script:MaxDiffChars = 60000

# --- helpers -----------------------------------------------------------------------------------

# Property access that works for both hashtables and PSCustomObjects (ConvertFrom-Json output).
function Get-Prop($Object, [string]$Name) {
    if ($null -eq $Object) { return $null }
    if ($Object -is [System.Collections.IDictionary]) { return $Object[$Name] }
    $p = $Object.PSObject.Properties[$Name]
    if ($p) { return $p.Value }
    return $null
}

function Write-TextFile([string]$Path, [string]$Content) {
    $dir = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    [System.IO.File]::WriteAllText($Path, $Content, $script:Utf8NoBom)
}

function Read-TextFile([string]$Path) {
    if (Test-Path -LiteralPath $Path) { return [System.IO.File]::ReadAllText($Path) }
    return ''
}

function New-CheckResult([string]$Text, [bool]$Passed, [string]$Evidence) {
    [pscustomobject]@{ text = $Text; passed = $Passed; evidence = $Evidence; source = 'check' }
}

# Runs a PowerShell command string in $WorkDir. -EncodedCommand avoids native-argument quote mangling
# on Windows PowerShell 5.1. Returns @(exitCode, output).
function Invoke-Shell([string]$WorkDir, [string]$Command) {
    $ErrorActionPreference = 'Continue'
    $exe = 'powershell'
    if (Get-Command pwsh -ErrorAction SilentlyContinue) { $exe = 'pwsh' }
    $script = "`$ProgressPreference = 'SilentlyContinue'`n$Command`nexit `$LASTEXITCODE"
    $encoded = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($script))
    Push-Location -LiteralPath $WorkDir
    try {
        $out = & $exe -NoProfile -NonInteractive -EncodedCommand $encoded 2>&1 | Out-String
        $code = $LASTEXITCODE
    }
    finally { Pop-Location }
    return @($code, $out)
}

function ConvertTo-GlobRegex([string]$Glob) {
    $sb = New-Object System.Text.StringBuilder('^')
    $i = 0
    while ($i -lt $Glob.Length) {
        $ch = $Glob[$i]
        if ($ch -eq '*' -and $i + 1 -lt $Glob.Length -and $Glob[$i + 1] -eq '*') {
            if ($i + 2 -lt $Glob.Length -and $Glob[$i + 2] -eq '/') { [void]$sb.Append('(.*/)?'); $i += 3 }
            else { [void]$sb.Append('.*'); $i += 2 }
            continue
        }
        if ($ch -eq '*') { [void]$sb.Append('[^/]*') }
        elseif ($ch -eq '?') { [void]$sb.Append('[^/]') }
        else { [void]$sb.Append([regex]::Escape([string]$ch)) }
        $i++
    }
    return $sb.Append('$').ToString()
}

# Files under $WorkDir whose repo-relative path matches $Glob. Enumerates only below the glob's literal prefix.
function Resolve-GlobFiles([string]$WorkDir, [string]$Glob) {
    $segments = $Glob.Split('/')
    $prefix = @()
    foreach ($s in $segments) { if ($s -match '[*?]') { break }; $prefix += $s }
    if ($prefix.Count -eq $segments.Count) { $prefix = $prefix[0..($prefix.Count - 2)] }
    $root = $WorkDir
    if ($prefix.Count -gt 0) { $root = Join-Path $WorkDir ($prefix -join '/') }
    if (-not (Test-Path -LiteralPath $root)) { return @() }
    $rx = ConvertTo-GlobRegex $Glob
    $base = (Get-Item -LiteralPath $WorkDir).FullName.TrimEnd('\', '/')
    foreach ($f in Get-ChildItem -LiteralPath $root -Recurse -File -ErrorAction SilentlyContinue) {
        $rel = $f.FullName.Substring($base.Length).TrimStart('\', '/').Replace('\', '/')
        if ($rel -match '(^|/)(bin|obj|node_modules)/') { continue }
        if ($rel -match $rx) { $f.FullName }
    }
}

function Get-WorkTreeHash([string]$WorkDir) {
    git -C $WorkDir add -A 2>$null | Out-Null
    return (git -C $WorkDir write-tree).Trim()
}

# --- eval sets ---------------------------------------------------------------------------------

function Read-SkillEvalSet {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)
    $raw = [System.IO.File]::ReadAllText($Path)
    try { $set = $raw | ConvertFrom-Json } catch { throw "Invalid JSON in ${Path}: $($_.Exception.Message)" }
    $folder = Split-Path -Leaf (Split-Path -Parent $Path)
    $skill = Get-Prop $set 'skill_name'
    if ($skill -cne $folder) { throw "skill_name '$skill' does not match folder '$folder'" }
    $evals = @(Get-Prop $set 'evals')
    if ($evals.Count -eq 0) { throw "$Path has no evals" }
    $ids = @{}
    foreach ($e in $evals) {
        $id = Get-Prop $e 'id'
        if ($null -eq $id) { throw 'an eval has no id' }
        if ($ids.ContainsKey("$id")) { throw "duplicate eval id $id" }
        $ids["$id"] = $true
        if (-not (Get-Prop $e 'name')) { throw "eval $id has no name" }
        if (-not (Get-Prop $e 'prompt')) { throw "eval $id has no prompt" }
        $checks = @(Get-Prop $e 'checks' | Where-Object { $_ })
        $expectations = @(Get-Prop $e 'expectations' | Where-Object { $_ })
        if ($checks.Count + $expectations.Count -eq 0) { throw "eval $id has neither checks nor expectations" }
        foreach ($c in $checks) {
            $t = Get-Prop $c 'type'
            if ($script:CheckTypes -notcontains $t) { throw "eval $id has unknown check type '$t' (known: $($script:CheckTypes -join ', '))" }
        }
    }
    return $set
}

function Get-ClaudeArgs {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][ValidateSet('run', 'grade')][string]$Purpose,
        [string]$Model,
        [int]$MaxTurns = 30
    )
    $a = @('-p', '--output-format', 'json')
    if ($Purpose -eq 'run') { $a += @('--max-turns', "$MaxTurns", '--permission-mode', 'acceptEdits', '--allowedTools', $script:RunTools) }
    else { $a += @('--max-turns', '1') }
    if ($Model) { $a += @('--model', $Model) }
    return $a
}

# --- checks ------------------------------------------------------------------------------------

function Invoke-EvalChecks {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$WorkDir,
        [AllowEmptyString()][string]$ResponseText = '',
        [AllowEmptyCollection()][object[]]$Checks = @(),
        [string]$SetupTree,
        [string]$BaseCommit
    )
    foreach ($c in $Checks) {
        $type = Get-Prop $c 'type'
        switch ($type) {
            'command' {
                $run = Get-Prop $c 'run'
                $expect = Get-Prop $c 'expect_exit'
                if ($null -eq $expect) { $expect = 0 }
                $r = Invoke-Shell $WorkDir $run
                $tail = (($r[1] -split "`r?`n") | Where-Object { $_.Trim() } | Select-Object -Last 5) -join ' | '
                New-CheckResult "command: $run" ($r[0] -eq $expect) ("exit {0} (expected {1}): {2}" -f $r[0], $expect, $tail)
            }
            'file-matches' {
                $glob = Get-Prop $c 'glob'; $pattern = Get-Prop $c 'pattern'
                $files = @(Resolve-GlobFiles $WorkDir $glob)
                $hit = $null
                foreach ($f in $files) { if ([regex]::IsMatch([System.IO.File]::ReadAllText($f), $pattern)) { $hit = $f; break } }
                $evidence = if ($hit) { "matched in $($hit.Substring($WorkDir.Length).TrimStart('\','/'))" } else { "$($files.Count) file(s) under '$glob', none match /$pattern/" }
                New-CheckResult "file-matches: $glob /$pattern/" ($null -ne $hit) $evidence
            }
            'no-edits' {
                $tree = Get-WorkTreeHash $WorkDir
                New-CheckResult 'no-edits' ($tree -eq $SetupTree) ("tree {0} vs setup {1}" -f $tree, $SetupTree)
            }
            'no-commit' {
                $head = (git -C $WorkDir rev-parse HEAD).Trim()
                New-CheckResult 'no-commit' ($head -eq $BaseCommit) ("HEAD {0} vs base {1}" -f $head, $BaseCommit)
            }
            'response-matches' {
                $pattern = Get-Prop $c 'pattern'
                $ok = [regex]::IsMatch($ResponseText, $pattern)
                New-CheckResult "response-matches: /$pattern/" $ok ($(if ($ok) { 'matched' } else { 'no match in final response' }))
            }
            default { New-CheckResult "unknown check '$type'" $false 'unknown check type' }
        }
    }
}

# --- grading -----------------------------------------------------------------------------------

# Parses claude -p JSON output from the grader. Returns $null when no expectations JSON is found.
function ConvertFrom-GraderOutput([string]$Raw) {
    if (-not $Raw) { return $null }
    $cost = 0.0; $duration = 0; $text = $Raw
    try {
        $outer = $Raw | ConvertFrom-Json
        if (Get-Prop $outer 'result') { $text = [string](Get-Prop $outer 'result') }
        if (Get-Prop $outer 'total_cost_usd') { $cost = [double](Get-Prop $outer 'total_cost_usd') }
        if (Get-Prop $outer 'duration_ms') { $duration = [int](Get-Prop $outer 'duration_ms') }
    }
    catch { $text = $Raw }
    $start = $text.IndexOf('{'); $end = $text.LastIndexOf('}')
    if ($start -lt 0 -or $end -le $start) { return $null }
    try { $inner = $text.Substring($start, $end - $start + 1) | ConvertFrom-Json } catch { return $null }
    $items = @(Get-Prop $inner 'expectations')
    if ($items.Count -eq 0) { return $null }
    return @{ Items = $items; Cost = $cost; Duration = $duration }
}

function Invoke-EvalGrade {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RunDir,
        [AllowEmptyCollection()][string[]]$Expectations = @(),
        [Parameter(Mandatory)][scriptblock]$Invoker,
        [Parameter(Mandatory)][string]$GraderPromptPath,
        [string]$Model
    )
    $results = New-Object System.Collections.Generic.List[object]
    $checksPath = Join-Path $RunDir 'checks.json'
    if (Test-Path -LiteralPath $checksPath) {
        foreach ($c in @([System.IO.File]::ReadAllText($checksPath) | ConvertFrom-Json)) {
            $results.Add([pscustomobject]@{ text = $c.text; passed = [bool]$c.passed; evidence = $c.evidence; source = 'check' })
        }
    }
    $cost = 0.0; $duration = 0
    $responseJson = Join-Path $RunDir 'outputs/response.json'
    if (Test-Path -LiteralPath $responseJson) {
        try {
            $r = [System.IO.File]::ReadAllText($responseJson) | ConvertFrom-Json
            if (Get-Prop $r 'total_cost_usd') { $cost += [double](Get-Prop $r 'total_cost_usd') }
            if (Get-Prop $r 'duration_ms') { $duration += [int](Get-Prop $r 'duration_ms') }
        }
        catch { }
    }
    $exp = @($Expectations | Where-Object { $_ })
    if ($exp.Count -gt 0) {
        $diff = Read-TextFile (Join-Path $RunDir 'outputs/diff.patch')
        if ($diff.Length -gt $script:MaxDiffChars) { $diff = $diff.Substring(0, $script:MaxDiffChars) + "`n[diff truncated]" }
        $numbered = for ($i = 0; $i -lt $exp.Count; $i++) { "{0}. {1}" -f ($i + 1), $exp[$i] }
        $fence = '```'
        $prompt = (Read-TextFile $GraderPromptPath) + "`n`n## Expectations`n" + ($numbered -join "`n") +
            "`n`n## Final response`n" + (Read-TextFile (Join-Path $RunDir 'outputs/response.md')) +
            "`n`n## Diff of the session's changes`n$fence" + "diff`n$diff`n$fence" +
            "`n`n## Deterministic check results`n$fence" + "json`n" + (Read-TextFile $checksPath) + "`n$fence`n"
        $raw = $null
        try { $raw = (& $Invoker -WorkDir $RunDir -Prompt $prompt -ClaudeArgs (Get-ClaudeArgs -Purpose grade -Model $Model)) | Out-String }
        catch { $raw = $null }
        $parsed = ConvertFrom-GraderOutput $raw
        for ($i = 0; $i -lt $exp.Count; $i++) {
            if ($null -eq $parsed) {
                $results.Add([pscustomobject]@{ text = $exp[$i]; passed = $false; evidence = 'grader returned unparseable output'; source = 'grader' })
                continue
            }
            $item = $parsed.Items | Where-Object { $_.text -eq $exp[$i] } | Select-Object -First 1
            if (-not $item -and $i -lt $parsed.Items.Count) { $item = $parsed.Items[$i] }
            if ($item) { $results.Add([pscustomobject]@{ text = $exp[$i]; passed = [bool]$item.passed; evidence = [string]$item.evidence; source = 'grader' }) }
            else { $results.Add([pscustomobject]@{ text = $exp[$i]; passed = $false; evidence = 'grader omitted this expectation'; source = 'grader' }) }
        }
        if ($parsed) { $cost += $parsed.Cost; $duration += $parsed.Duration }
    }
    $all = $results.ToArray()
    $passed = @($all | Where-Object { $_.passed }).Count
    $rate = 0.0
    if ($all.Count -gt 0) { $rate = [math]::Round($passed / $all.Count, 2) }
    $grading = [pscustomobject]@{
        expectations = $all
        pass_rate    = $rate
        cost_usd     = [math]::Round($cost, 4)
        duration_ms  = $duration
    }
    Write-TextFile (Join-Path $RunDir 'grading.json') ($grading | ConvertTo-Json -Depth 6)
    return $grading
}

# --- benchmark ---------------------------------------------------------------------------------

function Get-Average([double[]]$Values) {
    if ($Values.Count -eq 0) { return 0.0 }
    $sum = 0.0
    foreach ($v in $Values) { $sum += $v }
    return $sum / $Values.Count
}

function Get-EvalBenchmark {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$ResultsDir)
    $rows = @()
    foreach ($skillDir in Get-ChildItem -LiteralPath $ResultsDir -Directory) {
        $rates = @{ with_skill = @(); without_skill = @() }
        $cost = 0.0; $duration = 0; $evalCount = 0
        $nonDiscriminating = @()
        foreach ($evalDir in Get-ChildItem -LiteralPath $skillDir.FullName -Directory -Filter 'eval-*') {
            $evalCount++
            $byConfig = @{}
            foreach ($cfg in 'with_skill', 'without_skill') {
                $cfgDir = Join-Path $evalDir.FullName $cfg
                $gradings = @()
                if (Test-Path -LiteralPath $cfgDir) {
                    $gradings = @(Get-ChildItem -LiteralPath $cfgDir -Recurse -File -Filter 'grading.json' | ForEach-Object { [System.IO.File]::ReadAllText($_.FullName) | ConvertFrom-Json })
                }
                foreach ($g in $gradings) {
                    $rates[$cfg] += [double]$g.pass_rate
                    $cost += [double]$g.cost_usd
                    $duration += [int]$g.duration_ms
                }
                $byConfig[$cfg] = $gradings
            }
            $allRuns = @($byConfig['with_skill']) + @($byConfig['without_skill'])
            if (@($byConfig['with_skill']).Count -gt 0 -and @($byConfig['without_skill']).Count -gt 0) {
                $texts = @($allRuns | ForEach-Object { $_.expectations } | ForEach-Object { $_.text } | Select-Object -Unique)
                foreach ($t in $texts) {
                    $everywhere = $true
                    foreach ($run in $allRuns) {
                        $hit = @($run.expectations | Where-Object { $_.text -eq $t -and $_.passed })
                        if ($hit.Count -eq 0) { $everywhere = $false; break }
                    }
                    if ($everywhere) { $nonDiscriminating += $t }
                }
            }
        }
        $with = [math]::Round((Get-Average $rates['with_skill']), 2)
        $without = [math]::Round((Get-Average $rates['without_skill']), 2)
        $delta = [math]::Round($with - $without, 2)
        $verdict = 'neutral'
        if ($delta -ge 0.10) { $verdict = 'helps' } elseif ($delta -le -0.10) { $verdict = 'hurts' }
        $rows += [pscustomobject]@{
            skill                   = $skillDir.Name
            evals                   = $evalCount
            with_skill_pass_rate    = $with
            without_skill_pass_rate = $without
            delta                   = $delta
            verdict                 = $verdict
            cost_usd                = [math]::Round($cost, 4)
            duration_s              = [math]::Round($duration / 1000.0, 1)
            non_discriminating      = $nonDiscriminating
        }
    }
    Write-TextFile (Join-Path $ResultsDir 'benchmark.json') (ConvertTo-Json -InputObject @($rows) -Depth 4)
    $deltaSign = [string][char]0x0394
    $md = @('# Skill eval summary', '', "| Skill | Evals | With skill | Without | $deltaSign | Verdict | Cost (USD) | Time (s) |", '|---|---|---|---|---|---|---|---|')
    foreach ($r in $rows) {
        $md += "| {0} | {1} | {2:N2} | {3:N2} | {4:+0.00;-0.00;0.00} | {5} | {6:N4} | {7:N1} |" -f $r.skill, $r.evals, $r.with_skill_pass_rate, $r.without_skill_pass_rate, $r.delta, $r.verdict, $r.cost_usd, $r.duration_s
    }
    $md += ''
    $md += "Verdict: helps if $deltaSign >= +0.10, hurts if $deltaSign <= -0.10, otherwise neutral."
    foreach ($r in @($rows | Where-Object { @($_.non_discriminating).Count -gt 0 })) {
        $md += ''
        $md += "Non-discriminating expectations for $($r.skill) (pass with and without the skill):"
        foreach ($t in $r.non_discriminating) { $md += "- $t" }
    }
    Write-TextFile (Join-Path $ResultsDir 'summary.md') (($md -join "`n") + "`n")
    return $rows
}

Export-ModuleMember -Function Read-SkillEvalSet, Get-ClaudeArgs, Invoke-EvalChecks, Invoke-EvalGrade, Get-EvalBenchmark
