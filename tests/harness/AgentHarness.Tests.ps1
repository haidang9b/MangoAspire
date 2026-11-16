#Requires -Version 5.1
# Pester 5+ tests for scripts/harness/AgentHarness.psm1.
# Run: powershell -NoProfile -Command "Invoke-Pester -Path tests/harness -Output Detailed"

BeforeAll {
    Import-Module (Join-Path $PSScriptRoot '../../scripts/harness/AgentHarness.psm1') -Force
    function New-Repo([hashtable]$Files) {
        $root = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        foreach ($k in $Files.Keys) {
            $p = Join-Path $root $k
            New-Item -ItemType Directory -Force -Path (Split-Path $p) | Out-Null
            [IO.File]::WriteAllText($p, $Files[$k])
        }
        $root
    }
}

Describe 'Split-Frontmatter' {
    It 'separates front lines from body' {
        $r = Split-Frontmatter -Text "---`nname: x`n---`n`nBody"
        $r.Front | Should -Be @('name: x'); $r.Body.Trim() | Should -Be 'Body'
    }
    It 'handles CRLF input' { (Split-Frontmatter -Text "---`r`nname: x`r`n---`r`nB").Front | Should -Be @('name: x') }
    It 'returns empty front without frontmatter' { @((Split-Frontmatter -Text 'Body').Front).Count | Should -Be 0 }
}

Describe 'Get-FrontmatterList' {
    It 'reads a block list and strips quotes' {
        Get-FrontmatterList -Front @('paths:', '  - "src/**/*.cs"', "  - 'tests/**'") -Key paths | Should -Be @('src/**/*.cs', 'tests/**')
    }
    It 'reads an inline list' { Get-FrontmatterList -Front @('paths: [a/**, b/**]') -Key paths | Should -Be @('a/**', 'b/**') }
    It 'reads a comma scalar' { Get-FrontmatterList -Front @('paths: "a/**,b/**"') -Key paths | Should -Be @('a/**', 'b/**') }
    It 'returns empty when absent' { @(Get-FrontmatterList -Front @('name: x') -Key paths).Count | Should -Be 0 }
}

Describe 'ConvertTo-CopilotInstruction' {
    It 'maps paths to applyTo and keeps the body' {
        $out = ConvertTo-CopilotInstruction -Text "---`npaths:`n  - `"src/**/*.cs`"`n  - `"tests/**/*.cs`"`n---`n`n# Rule`nBody"
        $out | Should -Match '(?m)^applyTo: "src/\*\*/\*\.cs,tests/\*\*/\*\.cs"$'
        $out | Should -Match 'GENERATED from \.claude/'
        $out | Should -Match '# Rule'
        $out | Should -Not -Match 'paths:'
    }
    It 'throws when the rule has no paths' { { ConvertTo-CopilotInstruction -Text '# no front' } | Should -Throw }
}

Describe 'ConvertTo-CopilotAgent' {
    It 'keeps only name and description' {
        $out = ConvertTo-CopilotAgent -Text "---`nname: a`ndescription: d`ntools: Read, Grep`nmodel: opus`n---`nBody"
        $out | Should -Match '(?m)^name: a$'; $out | Should -Match '(?m)^description: d$'
        $out | Should -Not -Match 'tools:'; $out | Should -Not -Match 'model:'
    }
}

Describe 'Invoke-HarnessSync and Get-HarnessDrift' {
    BeforeEach {
        $repo = New-Repo @{
            '.claude/rules/backend.md'         = "---`npaths:`n  - `"src/**/*.cs`"`n---`nB"
            '.claude/rules/memory/identity.md' = "---`npaths:`n  - `"src/Services/Identity.API/**`"`n---`nI"
            '.claude/agents/code-reviewer.md'  = "---`nname: code-reviewer`ndescription: d`n---`nBody"
            '.github/workflows/build.yml'      = 'name: Build'
        }
    }
    It 'mirrors nested rules and agents' {
        Invoke-HarnessSync -RepoRoot $repo
        Join-Path $repo '.github/instructions/memory/identity.instructions.md' | Should -Exist
        Join-Path $repo '.github/agents/code-reviewer.agent.md' | Should -Exist
    }
    It 'leaves .github/workflows untouched' {
        Invoke-HarnessSync -RepoRoot $repo
        Join-Path $repo '.github/workflows/build.yml' | Should -Exist
    }
    It 'removes mirror files whose source is gone' {
        Invoke-HarnessSync -RepoRoot $repo
        Remove-Item (Join-Path $repo '.claude/agents/code-reviewer.md')
        Invoke-HarnessSync -RepoRoot $repo
        Join-Path $repo '.github/agents/code-reviewer.agent.md' | Should -Not -Exist
    }
    It 'reports no drift right after sync' {
        Invoke-HarnessSync -RepoRoot $repo
        @(Get-HarnessDrift -RepoRoot $repo).Count | Should -Be 0
    }
    It 'ignores CRLF line endings in the committed mirror' {
        Invoke-HarnessSync -RepoRoot $repo
        $f = Join-Path $repo '.github/instructions/backend.instructions.md'
        [IO.File]::WriteAllText($f, ([IO.File]::ReadAllText($f) -replace "`n", "`r`n"))
        @(Get-HarnessDrift -RepoRoot $repo).Count | Should -Be 0
    }
    It 'reports changed, extra and missing files' {
        Invoke-HarnessSync -RepoRoot $repo
        Add-Content (Join-Path $repo '.github/instructions/backend.instructions.md') 'hand edit'
        Set-Content (Join-Path $repo '.github/agents/ghost.agent.md') 'x'
        Remove-Item (Join-Path $repo '.github/agents/code-reviewer.agent.md')
        $d = Get-HarnessDrift -RepoRoot $repo
        $d | Should -Contain 'changed: instructions/backend.instructions.md'
        $d | Should -Contain 'extra: agents/ghost.agent.md'
        $d | Should -Contain 'missing: agents/code-reviewer.agent.md'
    }
}

Describe 'Invoke-HarnessLint' {
    BeforeEach {
        $repo = New-Repo @{
            'AGENTS.md'                            = "# Agents`nSee ``.agent/memory/index.md``."
            'CLAUDE.md'                            = "@AGENTS.md`n"
            '.agent/memory/index.md'               = '# Index'
            '.claude/rules/backend.md'             = "---`npaths:`n  - `"src/**/*.cs`"`n---`nSee ``src/**/*.cs`` and ``.agent/tickets/<ID>/ticket.md``."
            '.claude/skills/run-tests/SKILL.md'    = "---`nname: run-tests`ndescription: Use when running tests.`n---`nBody"
            '.claude/skills/fix-warnings/SKILL.md' = "---`nname: fix-warnings`ndescription: Use when fixing warnings.`ndisable-model-invocation: true`n---`nBody"
            '.claude/agents/code-reviewer.md'      = "---`nname: code-reviewer`ndescription: Reviews code.`n---`nFollow ``.claude/skills/run-tests/SKILL.md``; run ``/fix-warnings``."
        }
        Invoke-HarnessSync -RepoRoot $repo
        function Set-RepoFile($rel, $text) {
            $p = Join-Path $repo $rel; New-Item -ItemType Directory -Force (Split-Path $p) | Out-Null; [IO.File]::WriteAllText($p, $text)
        }
        function Lint($cat) { @(Invoke-HarnessLint -RepoRoot $repo -Category $cat) }
    }
    It 'reports nothing for a valid repo' { @(Invoke-HarnessLint -RepoRoot $repo).Count | Should -Be 0 }
    It 'flags AGENTS.md over budget' { Set-RepoFile 'AGENTS.md' ('x' * 4097); (Lint Budget).File | Should -Contain 'AGENTS.md' }
    It 'flags always-on total over budget via imports' {
        Set-RepoFile 'AGENTS.md' ('x' * 4000); Set-RepoFile '.agent/memory/index.md' ('y' * 1200)
        Set-RepoFile 'CLAUDE.md' "@AGENTS.md`n@.agent/memory/index.md`n"
        (Lint Budget).Message -join ' ' | Should -Match 'always-on'
    }
    It 'flags a rule without paths' { Set-RepoFile '.claude/rules/backend.md' '# no front'; @(Lint Rules).Count | Should -BeGreaterThan 0 }
    It 'flags prose in paths' {
        Set-RepoFile '.claude/rules/backend.md' "---`npaths: Only triggers for React files`n---`nB"
        @(Lint Rules).Count | Should -BeGreaterThan 0
    }
    It 'flags a skill name that differs from its folder' {
        Set-RepoFile '.claude/skills/run-tests/SKILL.md' "---`nname: run-project-tests`ndescription: Use when running tests.`n---`nB"
        @(Lint Skills).Count | Should -BeGreaterThan 0
    }
    It 'flags a description that is not trigger-only' {
        Set-RepoFile '.claude/skills/run-tests/SKILL.md' "---`nname: run-tests`ndescription: Runs the tests.`n---`nB"
        @(Lint Skills).Count | Should -BeGreaterThan 0
    }
    It 'flags more than six model-invocable skills' {
        1..6 | ForEach-Object { Set-RepoFile ".claude/skills/s$_/SKILL.md" "---`nname: s$_`ndescription: Use when $_.`n---`nB" }
        (Lint Skills).Message -join ' ' | Should -Match 'model-invocable'
    }
    It 'flags a bare reference to a user-only skill' {
        Set-RepoFile '.claude/agents/code-reviewer.md' "---`nname: code-reviewer`ndescription: d`n---`nThen run ``fix-warnings``."
        (Lint Skills).File | Should -Contain '.claude/agents/code-reviewer.md'
    }
    It 'flags an agent name that differs from its file' {
        Set-RepoFile '.claude/agents/code-reviewer.md' "---`nname: reviewer`ndescription: d`n---`nB"
        @(Lint Agents).Count | Should -BeGreaterThan 0
    }
    It 'flags a missing backticked path' {
        Set-RepoFile 'AGENTS.md' 'See `.agent/missing.md`.'
        (Lint Links).Message -join ' ' | Should -Match '\.agent/missing\.md'
    }
    It 'flags a broken relative markdown link in specs' {
        Set-RepoFile 'specs/a/spec.md' 'See [design](design.md).'
        (Lint Links).File | Should -Contain 'specs/a/spec.md'
    }
    It 'resolves anchored and root links and ignores code, placeholders and URLs' {
        Set-RepoFile '.agent/memory/index.md' @'
[i](index.md#top) [r](/AGENTS.md) `[x](nope.md)` [s](specs/<slug>/spec.md) [u](https://e.com) [h](#top)
~~~
[y](nope.md)
~~~
'@
        @(Lint Links).Count | Should -Be 0
    }
    It 'keeps a tilde line inside a backtick fence from closing it' {
        Set-RepoFile '.agent/memory/index.md' ("``````powershell`n~~~`n[y](nope.md)`n~~~`n``````")
        @(Lint Links).Count | Should -Be 0
    }
    It 'flags stale terms' {
        Set-RepoFile '.claude/skills/run-tests/SKILL.md' "---`nname: run-tests`ndescription: Use when testing.`n---`nThen npm run dev."
        @(Lint Staleness).Count | Should -Be 1
    }
    It 'does not flag pnpm install as npm install' {
        Set-RepoFile '.claude/skills/run-tests/SKILL.md' "---`nname: run-tests`ndescription: Use when testing.`n---`nRun pnpm install --dir x, then pnpm run build."
        @(Lint Staleness).Count | Should -Be 0
    }
    It 'flags drift' {
        Add-Content (Join-Path $repo '.github/agents/code-reviewer.agent.md') 'edit'
        (Lint Drift).File | Should -Contain '.github/agents/code-reviewer.agent.md'
    }
    It 'filters by category' { Set-RepoFile 'AGENTS.md' ('x' * 4097); @(Lint Rules).Count | Should -Be 0 }
}

Describe 'Get-HarnessContextReport' {
    It 'counts always-on bytes and skill kinds' {
        $repo = New-Repo @{
            'AGENTS.md' = 'abcd'; 'CLAUDE.md' = '@AGENTS.md'
            '.claude/skills/a/SKILL.md' = "---`nname: a`ndescription: Use when a.`n---`nB"
            '.claude/skills/b/SKILL.md' = "---`nname: b`ndescription: Use when b.`ndisable-model-invocation: true`n---`nB"
        }
        $r = Get-HarnessContextReport -RepoRoot $repo
        $r.AlwaysOnBytes | Should -Be 14; $r.ModelSkillCount | Should -Be 1; $r.UserSkillCount | Should -Be 1
    }
}

Describe 'Facts lint' {
    BeforeEach {
        $repo = New-Repo @{
            'src/Shared/Errors.cs'                    = 'public class DataVerificationException : Exception {} public record ResultModel<T>(T Data);'
            'src/UI/mango-ui/src/hooks/useFetch.ts'   = 'export function useFetch() {}'
            'src/UI/mango-ui/node_modules/x/index.js' = 'GhostService'
            'Directory.Packages.props'                = '<PackageVersion Include="Shouldly" />'
            'scripts/harness/facts-allowlist.txt'     = "GetWidgetById  # illustrative name in naming examples`n"
        }
        function Facts($agents) { [IO.File]::WriteAllText((Join-Path $repo 'AGENTS.md'), $agents); @(Invoke-HarnessLint -RepoRoot $repo -Category Facts) }
    }
    It 'accepts symbols, generics, calls and packages found in the corpus' {
        @(Facts 'Use `DataVerificationException`, `ResultModel<T>`, `useFetch()` and `Shouldly`.').Count | Should -Be 0
    }
    It 'flags a symbol that exists only in node_modules' { (Facts 'Call `GhostService`.').Message | Should -Match 'GhostService' }
    It 'skips file names, lowercase words, acronyms and fenced code' {
        @(Facts "Read ``AGENTS.md`` and ``Directory.Packages.props``; run ``pnpm``; ``CRLF``.`n~~~`n``GhostService```n~~~").Count | Should -Be 0
    }
    It 'skips six-letter extensions such as .cshtml and .csproj' {
        @(Facts 'Edit `_Layout.cshtml` and `Products.API.csproj`.').Count | Should -Be 0
    }
    It 'honours the allowlist' { @(Facts 'e.g. `GetWidgetById`').Count | Should -Be 0 }
    It 'flags an allowlist entry without a reason' {
        [IO.File]::WriteAllText((Join-Path $repo 'scripts/harness/facts-allowlist.txt'), "GetWidgetById`n")
        (Facts '# ok').File | Should -Contain 'scripts/harness/facts-allowlist.txt'
    }
    It 'does not scan specs' {
        New-Item -ItemType Directory -Force (Join-Path $repo 'specs/x') | Out-Null
        [IO.File]::WriteAllText((Join-Path $repo 'specs/x/spec.md'), 'Add `CouponExpiryJob`.')
        @(Facts '# ok').Count | Should -Be 0
    }
}

Describe 'Specs lint' {
    BeforeEach {
        $repo = New-Repo @{
            'specs/_template/spec.md'                = '**Status:** draft | in-review | approved | shipped'
            'specs/_template/acceptance-criteria.md' = '- [ ] AC-1: Given'
            'specs/_template/test-plan.md'           = '| AC-1 | x |'
            'specs/a/spec.md'                        = "# A`n`n**Status:** approved`n"
            'specs/a/acceptance-criteria.md'         = "- [ ] AC-1: Given a when b then c`n- [x] AC-2: Given d when e then f`n"
            'specs/a/test-plan.md'                   = "| AC | Unit |`n|---|---|`n| AC-1 | x |`n| AC-2 | x |`n"
        }
        function Set-Spec($rel, $text) { [IO.File]::WriteAllText((Join-Path $repo "specs/a/$rel"), $text) }
        function Specs { @(Invoke-HarnessLint -RepoRoot $repo -Category Specs) }
    }
    It 'accepts a complete spec' { @(Specs).Count | Should -Be 0 }
    It 'flags an invalid status' { Set-Spec 'spec.md' '**Status:** done'; @(Specs).Count | Should -Be 1 }
    It 'flags duplicate AC ids' { Set-Spec 'acceptance-criteria.md' "- [ ] AC-1: x`n- [ ] AC-1: y`n"; (Specs).Message -join ' ' | Should -Match 'AC-1' }
    It 'flags an AC missing from the test plan' { Set-Spec 'test-plan.md' "| AC-1 | x |`n"; (Specs).Message -join ' ' | Should -Match 'AC-2' }
    It 'flags an undefined AC in the test plan' { Set-Spec 'test-plan.md' "| AC-1 | x |`n| AC-2 | x |`n| AC-3 | x |`n"; (Specs).Message -join ' ' | Should -Match 'AC-3' }
    It 'flags a missing spec file' { Remove-Item (Join-Path $repo 'specs/a/test-plan.md'); @(Specs).Count | Should -BeGreaterThan 0 }
}

Describe 'Evals lint' {
    It 'flags an eval set for a missing skill and invalid JSON' {
        $repo = New-Repo @{
            '.claude/skills/run-tests/SKILL.md'      = "---`nname: run-tests`ndescription: Use when testing.`n---`nB"
            'tests/skill-evals/ghost/evals.json'     = '{"skill_name":"ghost","evals":[{"id":1,"name":"a","prompt":"p","expectations":["e"]}]}'
            'tests/skill-evals/run-tests/evals.json' = '{ not json'
            'tests/skill-evals/_grader.md'           = 'g'
        }
        @(Invoke-HarnessLint -RepoRoot $repo -Category Evals).File | Should -Be @('tests/skill-evals/ghost/evals.json', 'tests/skill-evals/run-tests/evals.json')
    }
}

Describe 'Harness scripts' {
    It 'are ASCII-only, because Windows PowerShell 5.1 reads BOM-less files as ANSI' {
        $root = Join-Path $PSScriptRoot '../..'
        $files = @(Get-ChildItem (Join-Path $root 'scripts/harness') -File -Filter *.psm1) + @(Get-ChildItem $PSScriptRoot -File -Filter *.ps1)
        $files += @('sync-agent-harness.ps1', 'eval-skills.ps1') | ForEach-Object { Join-Path $root "scripts/$_" } | Where-Object { Test-Path $_ } | ForEach-Object { Get-Item $_ }
        $bad = @($files | Where-Object { [IO.File]::ReadAllBytes($_.FullName) | Where-Object { $_ -gt 127 } | Select-Object -First 1 } | ForEach-Object { $_.Name })
        $bad | Should -BeNullOrEmpty
    }
}
