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
