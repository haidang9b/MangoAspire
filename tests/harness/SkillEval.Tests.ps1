#Requires -Version 5.1
# Pester 5+ tests for scripts/harness/SkillEval.psm1 (skill output evals).
# Run: powershell -NoProfile -Command "Invoke-Pester -Path tests/harness -Output Detailed"

BeforeAll {
    Import-Module (Join-Path $PSScriptRoot '../../scripts/harness/SkillEval.psm1') -Force
    function New-Dir([hashtable]$Files) {
        $root = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        foreach ($k in $Files.Keys) { $p = Join-Path $root $k; New-Item -ItemType Directory -Force (Split-Path $p) | Out-Null; [IO.File]::WriteAllText($p, $Files[$k]) }
        $root
    }
    $gp = Join-Path $TestDrive 'grader.md'; Set-Content $gp 'Grade.'
    $valid = '{"skill_name":"run-tests","evals":[{"id":1,"name":"one","prompt":"p","checks":[{"type":"no-edits"}],"expectations":["e"]}]}'
}

Describe 'Read-SkillEvalSet' {
    It 'reads a valid set' { $d = New-Dir @{ 'run-tests/evals.json' = $valid }; (Read-SkillEvalSet -Path "$d/run-tests/evals.json").evals[0].name | Should -Be 'one' }
    It 'rejects <case>' -TestCases @(
        @{ case = 'duplicate ids';   json = '{"skill_name":"run-tests","evals":[{"id":1,"name":"a","prompt":"p","expectations":["e"]},{"id":1,"name":"b","prompt":"p","expectations":["e"]}]}' }
        @{ case = 'missing prompt';  json = '{"skill_name":"run-tests","evals":[{"id":1,"name":"a","expectations":["e"]}]}' }
        @{ case = 'no assertions';   json = '{"skill_name":"run-tests","evals":[{"id":1,"name":"a","prompt":"p"}]}' }
        @{ case = 'unknown check';   json = '{"skill_name":"run-tests","evals":[{"id":1,"name":"a","prompt":"p","checks":[{"type":"vibes"}]}]}' }
        @{ case = 'folder mismatch'; json = '{"skill_name":"code-review","evals":[{"id":1,"name":"a","prompt":"p","expectations":["e"]}]}' }
    ) { param($case, $json) $d = New-Dir @{ 'run-tests/evals.json' = $json }; { Read-SkillEvalSet -Path "$d/run-tests/evals.json" } | Should -Throw }
}

Describe 'Get-ClaudeArgs' {
    It 'builds run args' {
        $a = Get-ClaudeArgs -Purpose run -MaxTurns 30
        ($a -join ' ') | Should -Match '^-p --output-format json --max-turns 30 --permission-mode acceptEdits'
        $a | Should -Contain '--allowedTools'; $a | Should -Not -Contain '--model'
    }
    It 'builds one-turn, tool-less grader args' {
        $a = Get-ClaudeArgs -Purpose grade -Model 'claude-sonnet-5-5'
        ($a -join ' ') | Should -Match '--max-turns 1'; $a | Should -Not -Contain '--allowedTools'; $a | Should -Contain 'claude-sonnet-5-5'
    }
    It 'allows the PowerShell tool every command it allows in Bash' {
        $a = Get-ClaudeArgs -Purpose run
        $tools = $a[[array]::IndexOf($a, '--allowedTools') + 1] -split ','
        $bash = @($tools | Where-Object { $_ -like 'Bash(*' })
        $bash.Count | Should -BeGreaterThan 0
        foreach ($b in $bash) { $tools | Should -Contain ($b -replace '^Bash\(', 'PowerShell(') }
    }
    It 'allows dotnet format and the harness lint the repo guidance asks for' {
        $a = Get-ClaudeArgs -Purpose run
        $tools = $a[[array]::IndexOf($a, '--allowedTools') + 1] -split ','
        $tools | Should -Contain 'PowerShell(dotnet format:*)'
        $tools | Should -Contain 'PowerShell(./scripts/sync-agent-harness.ps1 -Check:*)'
        $tools | Should -Contain 'Bash(powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync-agent-harness.ps1 -Check:*)'
    }
}

Describe 'Invoke-EvalChecks' {
    BeforeEach { $wd = New-Dir @{ 'tests/X.Tests/FooTests.cs' = 'public void Get_When_Missing_Then_NotFound() {}' } }
    It 'passes file-matches and response-matches' {
        $r = Invoke-EvalChecks -WorkDir $wd -ResponseText 'fix(products): guard null' -Checks @(
            @{ type = 'file-matches'; glob = 'tests/**/*Tests.cs'; pattern = '_When_\w+_Then_' },
            @{ type = 'response-matches'; pattern = '^fix\(' })
        $r.passed | Should -Be @($true, $true)
    }
    It 'limits file-matches to the files the session changed' {
        $chk = @(@{ type = 'file-matches'; glob = 'tests/**/*Tests.cs'; pattern = '_When_' })
        (@(Invoke-EvalChecks -WorkDir $wd -Checks $chk -ChangedFiles @()))[0].passed | Should -BeFalse
        (@(Invoke-EvalChecks -WorkDir $wd -Checks $chk -ChangedFiles @('tests/X.Tests/FooTests.cs')))[0].passed | Should -BeTrue
    }
    It 'fails a command with non-zero exit and records evidence' {
        $r = @(Invoke-EvalChecks -WorkDir $wd -ResponseText '' -Checks @(@{ type = 'command'; run = 'exit 3' }))
        $r[0].passed | Should -BeFalse; $r[0].evidence | Should -Match '3'
    }
}

Describe 'Invoke-EvalGrade' {
    BeforeEach { $run = New-Dir @{ 'outputs/response.md' = 'answer'; 'outputs/diff.patch' = ''; 'outputs/response.json' = '{"total_cost_usd":0.02,"duration_ms":10}'; 'checks.json' = '[{"text":"no-edits","passed":true,"evidence":"clean","source":"check"}]' } }
    It 'merges check results with grader verdicts' {
        $inv = { param($WorkDir, $Prompt, $ClaudeArgs) '{"result":"{\"expectations\":[{\"text\":\"e\",\"passed\":false,\"evidence\":\"missing\"}]}","total_cost_usd":0.01,"duration_ms":5}' }
        $g = Invoke-EvalGrade -RunDir $run -Expectations @('e') -Invoker $inv -GraderPromptPath $gp
        @($g.expectations).Count | Should -Be 2; $g.pass_rate | Should -Be 0.5; $g.cost_usd | Should -Be 0.03
    }
    It 'counts each deterministic check separately, including a failed one' {
        [IO.File]::WriteAllText((Join-Path $run 'checks.json'), '[{"text":"no-edits","passed":true,"evidence":"clean","source":"check"},{"text":"response-matches","passed":false,"evidence":"no match","source":"check"}]')
        $g = Invoke-EvalGrade -RunDir $run -Expectations @() -Invoker { throw 'must not be called' } -GraderPromptPath $gp
        @($g.expectations).Count | Should -Be 2
        $g.pass_rate | Should -Be 0.5
    }
    It 'retries the grader once when its first output is unparseable' {
        $calls = @{ n = 0 }
        $inv = { param($WorkDir, $Prompt, $ClaudeArgs)
            $calls.n++
            if ($calls.n -eq 1) { 'not json' }
            else { '{"result":"{\"expectations\":[{\"text\":\"e\",\"passed\":true,\"evidence\":\"ok\"}]}","total_cost_usd":0.01,"duration_ms":5}' } }.GetNewClosure()
        $g = Invoke-EvalGrade -RunDir $run -Expectations @('e') -Invoker $inv -GraderPromptPath $gp
        $calls.n | Should -Be 2
        ($g.expectations | Where-Object source -eq 'grader').passed | Should -BeTrue
    }
    It 'saves the raw grader output for diagnosis' {
        Invoke-EvalGrade -RunDir $run -Expectations @('e') -Invoker { 'not json' } -GraderPromptPath $gp | Out-Null
        Get-Content (Join-Path $run 'grader-raw.txt') -Raw | Should -Match 'not json'
    }
    It 'does not call the grader when the run failed' {
        Set-Content (Join-Path $run 'error.txt') 'boom'
        $g = Invoke-EvalGrade -RunDir $run -Expectations @('e') -Invoker { throw 'must not be called' } -GraderPromptPath $gp
        ($g.expectations | Where-Object source -eq 'grader').evidence | Should -Match 'run failed'
    }
    It 'grades in an empty folder outside the run directory' {
        $seen = @{}
        $inv = { param($WorkDir, $Prompt, $ClaudeArgs)
            $seen.dir = $WorkDir; $seen.count = @(Get-ChildItem -LiteralPath $WorkDir -Force).Count
            '{"result":"{\"expectations\":[{\"text\":\"e\",\"passed\":true,\"evidence\":\"ok\"}]}"}' }.GetNewClosure()
        Invoke-EvalGrade -RunDir $run -Expectations @('e') -Invoker $inv -GraderPromptPath $gp | Out-Null
        $seen.dir.StartsWith($run) | Should -BeFalse
        $seen.count | Should -Be 0
        Test-Path -LiteralPath $seen.dir | Should -BeFalse
    }
    It 'records unparseable grader output as failed' {
        $g = Invoke-EvalGrade -RunDir $run -Expectations @('e') -Invoker { 'not json' } -GraderPromptPath $gp
        ($g.expectations | Where-Object source -eq 'grader').evidence | Should -Match 'unparseable'
    }
    It 'skips the grader when there are no expectations' {
        $g = Invoke-EvalGrade -RunDir $run -Expectations @() -Invoker { throw 'must not be called' } -GraderPromptPath $gp
        $g.pass_rate | Should -Be 1
    }
}

Describe 'Get-EvalBenchmark' {
    It 'computes pass rates, delta, verdict and non-discriminating expectations' {
        $res = New-Dir @{
            'run-tests/eval-1-one/with_skill/run-1/grading.json'    = '{"pass_rate":1.0,"cost_usd":0.2,"duration_ms":1000,"expectations":[{"text":"a","passed":true},{"text":"b","passed":true}]}'
            'run-tests/eval-1-one/without_skill/run-1/grading.json' = '{"pass_rate":0.5,"cost_usd":0.1,"duration_ms":1000,"expectations":[{"text":"a","passed":true},{"text":"b","passed":false}]}'
        }
        $b = @(Get-EvalBenchmark -ResultsDir $res)
        $b[0].delta | Should -Be 0.5; $b[0].verdict | Should -Be 'helps'; $b[0].non_discriminating | Should -Be @('a')
        Join-Path $res 'summary.md' | Should -Exist
    }
}

Describe 'Get-EvalBenchmark health' {
    It 'reports run health per skill' {
        $ok = '{"pass_rate":1.0,"cost_usd":0.1,"duration_ms":1,"expectations":[{"text":"a","passed":true}]}'
        $res = New-Dir @{
            'run-tests/eval-1-one/with_skill/run-1/grading.json'    = $ok
            'run-tests/eval-1-one/with_skill/run-1/meta.json'       = '{"health":{"subtype":"error_max_turns","is_error":true,"num_turns":31,"permission_denials":2,"run_error":null}}'
            'run-tests/eval-1-one/without_skill/run-1/grading.json' = $ok
            'run-tests/eval-1-one/without_skill/run-1/meta.json'    = '{"health":{"subtype":"success","is_error":false,"num_turns":3,"permission_denials":1,"run_error":null}}'
        }
        $b = @(Get-EvalBenchmark -ResultsDir $res)
        $b[0].permission_denials | Should -Be 3
        $b[0].max_turns_hits | Should -Be 1
        $b[0].run_errors | Should -Be 0
        Get-Content (Join-Path $res 'summary.md') -Raw | Should -Match 'Denials'
    }
}

Describe 'Invoke-ClaudeCli' {
    It 'sends the prompt as UTF-8 and decodes the reply as UTF-8' {
        $d = New-Dir @{ 'x.txt' = 'x' }; git -C $d init -q
        $text = 'A' + [char]0x2013 + 'B' + [char]0x0393 + 'C'
        $sha = (Invoke-ClaudeCli -WorkDir $d -Prompt $text -ClaudeArgs @('hash-object', '-w', '--stdin') -Exe git).Trim()
        $back = Invoke-ClaudeCli -WorkDir $d -Prompt '' -ClaudeArgs @('cat-file', '-p', $sha) -Exe git
        $back.TrimEnd() | Should -Be $text
    }
}

Describe 'Invoke-EvalRun' {
    BeforeEach {
        $repo = New-Dir @{ '.claude/skills/run-tests/SKILL.md' = "---`nname: run-tests`ndescription: Use when testing.`n---`nB"; 'README.md' = 'x' }
        git -C $repo init -q; git -C $repo add -A; git -C $repo -c user.name=t -c user.email=t@t commit -qm init
        $eval = [pscustomobject]@{ id = 1; name = 'one'; prompt = 'run the tests'; setup = @(); checks = @([pscustomobject]@{ type = 'no-edits' }); expectations = @() }
        $seen = @{}
        $inv = { param($WorkDir, $Prompt, $ClaudeArgs)
            $seen.prompt = $Prompt; $seen.skillVisible = Test-Path (Join-Path $WorkDir '.claude/skills/run-tests')
            Set-Content (Join-Path $WorkDir 'scratch.txt') 'edit'
            '{"result":"done","total_cost_usd":0.01,"duration_ms":5}' }.GetNewClosure()
        $out = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        function Run($config, $invoker) { Invoke-EvalRun -RepoRoot $repo -SkillName run-tests -Eval $eval -Config $config -RunIndex 1 -OutDir $out -Invoker $invoker | Out-Null }
    }
    It 'sends the slash command with the skill present' { Run with_skill $inv; $seen.prompt | Should -Be '/run-tests run the tests'; $seen.skillVisible | Should -BeTrue }
    It 'hides the skill and sends the plain prompt without it' { Run without_skill $inv; $seen.prompt | Should -Be 'run the tests'; $seen.skillVisible | Should -BeFalse }
    It 'leaves the main working tree clean and removes the worktree' {
        Run with_skill $inv
        git -C $repo status --porcelain | Should -BeNullOrEmpty
        @(git -C $repo worktree list).Count | Should -Be 1
    }
    It 'checks file-matches only against files the session changed' {
        $eval.checks = @([pscustomobject]@{ type = 'file-matches'; glob = '**/*.md'; pattern = 'x' })
        Run with_skill $inv
        $c = [IO.File]::ReadAllText((Join-Path $out 'checks.json')) | ConvertFrom-Json
        @($c)[0].passed | Should -BeFalse
    }
    It 'saves the diff byte-exact, including non-ASCII text' {
        $inv2 = { param($WorkDir, $Prompt, $ClaudeArgs)
            [IO.File]::WriteAllText((Join-Path $WorkDir 'note.md'), ('dash ' + [char]0x2013), (New-Object System.Text.UTF8Encoding $false))
            '{"result":"done"}' }
        $prev = $null
        try { $prev = [Console]::OutputEncoding; [Console]::OutputEncoding = [Text.Encoding]::GetEncoding(28591) } catch { $prev = $null }
        try { Run with_skill $inv2 } finally { if ($prev) { try { [Console]::OutputEncoding = $prev } catch { } } }
        $hex = -join ([IO.File]::ReadAllBytes((Join-Path $out 'outputs/diff.patch')) | ForEach-Object { $_.ToString('X2') })
        $hex | Should -Match 'E28093'
    }
    It 'records run health and blanks the response when the run returned no result' {
        $inv3 = { param($WorkDir, $Prompt, $ClaudeArgs) '{"type":"result","subtype":"error_max_turns","is_error":true,"num_turns":31,"permission_denials":[{"tool_name":"PowerShell"},{"tool_name":"Bash"}]}' }
        Run with_skill $inv3
        $m = [IO.File]::ReadAllText((Join-Path $out 'meta.json')) | ConvertFrom-Json
        $m.health.subtype | Should -Be 'error_max_turns'
        $m.health.permission_denials | Should -Be 2
        $m.health.num_turns | Should -Be 31
        [IO.File]::ReadAllText((Join-Path $out 'outputs/response.md')) | Should -BeNullOrEmpty
    }
    It 'removes the worktree when the invoker throws' {
        Run with_skill { throw 'boom' }
        @(git -C $repo worktree list).Count | Should -Be 1
        Get-Content (Join-Path $out 'error.txt') -Raw | Should -Match 'boom'
    }
    It 'fails no-edits when the session edits files and saves outputs' {
        Run with_skill $inv
        (Get-Content (Join-Path $out 'checks.json') -Raw | ConvertFrom-Json)[0].passed | Should -BeFalse
        Get-Content (Join-Path $out 'outputs/diff.patch') -Raw | Should -Match 'scratch.txt'
        Join-Path $out 'outputs/response.json' | Should -Exist
    }
}

Describe 'Invoke-EvalRun index handling' {
    It 'leaves setup changes unstaged when the setup did not stage them' {
        $repo = New-Dir @{ '.claude/skills/run-tests/SKILL.md' = "---`nname: run-tests`ndescription: Use when testing.`n---`nB"; 'README.md' = 'x' }
        git -C $repo init -q; git -C $repo add -A; git -C $repo -c user.name=t -c user.email=t@t commit -qm init
        $eval = [pscustomobject]@{ id = 1; name = 'one'; prompt = 'review'; setup = @("Set-Content README.md 'changed'"); checks = @([pscustomobject]@{ type = 'no-edits' }); expectations = @() }
        $seen = @{}
        $inv = { param($WorkDir, $Prompt, $ClaudeArgs) $seen.unstaged = (git -C $WorkDir diff --name-only) -join ','; '{"result":"ok"}' }.GetNewClosure()
        $out = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        Invoke-EvalRun -RepoRoot $repo -SkillName run-tests -Eval $eval -Config with_skill -OutDir $out -Invoker $inv | Out-Null
        $seen.unstaged | Should -Be 'README.md'
        (Get-Content (Join-Path $out 'checks.json') -Raw | ConvertFrom-Json)[0].passed | Should -BeTrue
    }
}

Describe 'Invoke-EvalRun answer hiding' {
    It 'hides tests/skill-evals from the session without dirtying git status' {
        $repo = New-Dir @{ '.claude/skills/run-tests/SKILL.md' = "---`nname: run-tests`ndescription: Use when testing.`n---`nB"; 'tests/skill-evals/run-tests/evals.json' = $valid }
        git -C $repo init -q; git -C $repo add -A; git -C $repo -c user.name=t -c user.email=t@t commit -qm init
        $eval = [pscustomobject]@{ id = 1; name = 'one'; prompt = 'p'; setup = @(); checks = @([pscustomobject]@{ type = 'no-edits' }); expectations = @() }
        $seen = @{}
        $inv = { param($WorkDir, $Prompt, $ClaudeArgs)
            $seen.visible = Test-Path (Join-Path $WorkDir 'tests/skill-evals/run-tests/evals.json')
            $seen.status = (git -C $WorkDir status --porcelain) -join ','
            '{"result":"ok"}' }.GetNewClosure()
        $out = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        Invoke-EvalRun -RepoRoot $repo -SkillName run-tests -Eval $eval -Config with_skill -OutDir $out -Invoker $inv | Out-Null
        $seen.visible | Should -BeFalse
        $seen.status | Should -BeNullOrEmpty
        (Get-Content (Join-Path $out 'checks.json') -Raw | ConvertFrom-Json)[0].passed | Should -BeTrue
    }
}

Describe 'Invoke-EvalRun skill removal' {
    It 'does not let a setup git add -A stage the removed skill in without_skill runs' {
        $repo = New-Dir @{ '.claude/skills/run-tests/SKILL.md' = "---`nname: run-tests`ndescription: Use when testing.`n---`nB"; 'README.md' = 'x' }
        git -C $repo init -q; git -C $repo add -A; git -C $repo -c user.name=t -c user.email=t@t commit -qm init
        $eval = [pscustomobject]@{ id = 1; name = 'one'; prompt = 'p'; setup = @('git add -A'); checks = @([pscustomobject]@{ type = 'no-edits' }); expectations = @() }
        $seen = @{}
        $inv = { param($WorkDir, $Prompt, $ClaudeArgs)
            $seen.visible = Test-Path (Join-Path $WorkDir '.claude/skills/run-tests')
            $seen.staged = (git -C $WorkDir diff --cached --name-only) -join ','
            $seen.status = (git -C $WorkDir status --porcelain) -join ','
            '{"result":"ok"}' }.GetNewClosure()
        $out = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        Invoke-EvalRun -RepoRoot $repo -SkillName run-tests -Eval $eval -Config without_skill -OutDir $out -Invoker $inv | Out-Null
        $seen.visible | Should -BeFalse
        $seen.staged | Should -BeNullOrEmpty
        $seen.status | Should -BeNullOrEmpty
    }
}

Describe 'Invoke-SkillEvals -DryRun' {
    It 'lists every planned run and invokes nothing' {
        $repo = New-Dir @{ '.claude/skills/run-tests/SKILL.md' = "---`nname: run-tests`ndescription: Use when testing.`n---`nB"; 'tests/skill-evals/run-tests/evals.json' = $valid }
        $lines = Invoke-SkillEvals -RepoRoot $repo -Skill run-tests -Runs 2 -Invoker { throw 'no' } -DryRun 6>&1
        @($lines | Where-Object { "$_" -match 'claude -p' }).Count | Should -Be 4
    }
}
