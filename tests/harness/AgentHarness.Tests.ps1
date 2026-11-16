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
