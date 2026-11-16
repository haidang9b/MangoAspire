# AI-Native Harness Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give Claude Code and GitHub Copilot one canonical, token-lean set of rules, skills and agents. Fact-check the AI-written guidance in CI, adopt agentic-devkit specs, and measure each skill's value against a no-skill baseline.

**Architecture:** `.claude/` becomes canonical, and a PowerShell module mirrors rules and agents to `.github/` for Copilot. The same module lints budgets, links, staleness, drift, facts, specs and evals; CI runs it. A second module runs skill output evals in disposable git worktrees through `claude -p` and grades them. `specs/<slug>/` carries the agentic-devkit spec format.

**Tech Stack:** Markdown + YAML frontmatter, Windows PowerShell 5.1 / PowerShell 7, Pester 5, git worktrees, `claude` CLI, GitHub Actions.

**Spec:** `specs/ai-native-harness/spec.md`, `acceptance-criteria.md` (AC-1…AC-12), `test-plan.md`, `design.md`

## Global Constraints

- Branch `chore/HARNESS-NATIVE-001-ai-native-harness`, cut from `master`.
- Everything runs unchanged under Windows PowerShell 5.1 and PowerShell 7. Do not use `??`, `?.`, the ternary operator, `-AsHashtable` or a YAML module. Keep `#Requires -Version 5.1`.
- Locally, run scripts as `powershell -NoProfile -ExecutionPolicy Bypass -File <script>` (there is no `pwsh`). CI uses `pwsh`.
- Generated mirror files are UTF-8 without BOM with LF endings. The banner `<!-- GENERATED from .claude/ by scripts/sync-agent-harness.ps1 - do not edit directly -->` sits directly after the frontmatter.
- Budgets: `AGENTS.md` ≤ 4,096 B · `CLAUDE.md` ≤ 1,024 B · always-on ≤ 5,120 B · rule ≤ 4,096 B with ≥ 1 glob · `SKILL.md` ≤ 6,144 B · skill `description` ≤ 300 chars starting `Use when` · ≤ 6 model-invocable skills · agent body ≤ 3,072 B.
- **Harness set:** `AGENTS.md`, `CLAUDE.md`, `.claude/**/*.md`, `.agent/README.md`, `.agent/tools/*.md`, `.agent/memory/**/*.md`.
  **Markdown-link set:** the harness set plus `specs/**/*.md`.
- Skill folder = frontmatter `name` (kebab-case); agent file = `name`.
- Evals live in `tests/skill-evals/<skill>/`. Results go to `tests/skill-evals/_results/`, which is gitignored. No `claude` call ever runs in CI.
- Never hand-edit `.github/instructions/**` or `.github/agents/**`. Do not edit `docs/**`. Under `.agent/tickets/`, edit only `HARNESS-NATIVE-001/` and `_templates/feature.md`.
- Commits use Conventional Commits with scope `harness` and end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

1. The eval runner must leave the developer's working tree untouched, and must remove its worktree even when `claude` throws or the session edits or commits. → Task 10 tests `leaves the main working tree clean and removes the worktree` and `removes the worktree when the invoker throws`.
2. Facts must not flag file names (`AGENTS.md`), lowercase tool names, acronyms, fenced templates or specs describing future code, but it must flag a hallucinated symbol, even one that appears only in `node_modules`. → Task 3 tests.
3. Grader output that isn't JSON must mark those expectations failed, with evidence, and never crash the run. → Task 9 test `records unparseable grader output as failed`.
4. On a CRLF checkout (`* text=auto`), `-Check` must report no drift. → Task 1 test `ignores CRLF line endings`.
5. A rule with missing or prose `paths` must be a finding, because Claude silently makes it always-on. → Task 2 tests `flags a rule without paths` and `flags prose in paths`.

---

### Task 1: Harness module — parsing, Copilot mirror, drift

**ACs:** AC-3, AC-4

**Files:**
- Create: `scripts/harness/AgentHarness.psm1`
- Create: `tests/harness/AgentHarness.Tests.ps1`

**Interfaces:**
- Produces (exported):
  - `Split-Frontmatter -Text <string>` → `@{ Front = [string[]]; Body = [string] }`. Front excludes the fences and accepts CRLF.
  - `Get-FrontmatterValue -Front <string[]> -Key <string>` → `[string]` with quotes stripped, or `$null`.
  - `Get-FrontmatterList -Front <string[]> -Key <string>` → `[string[]]`. Accepts a block list, an inline `[a, b]` list or a comma scalar `"a,b"`. Returns `@()` when the key is absent.
  - `ConvertTo-CopilotInstruction -Text <string>` → `[string]`: `applyTo: "<globs joined by ,>"`, then the banner, then the body. Throws when `paths` is empty.
  - `ConvertTo-CopilotAgent -Text <string>` → `[string]`: frontmatter keeps only `name` and `description`, then the banner, then the body.
  - `Invoke-HarnessGenerate -RepoRoot <string> -OutRoot <string>` writes `<OutRoot>/instructions/<rel>.instructions.md` per `.claude/rules/**/*.md` (subfolders kept) and `<OutRoot>/agents/<name>.agent.md` per `.claude/agents/*.md`.
  - `Invoke-HarnessSync -RepoRoot <string>` deletes only `.github/instructions` and `.github/agents`, then generates into `.github`.
  - `Get-HarnessDrift -RepoRoot <string>` → sorted `[string[]]` of `missing: <rel>`, `changed: <rel>` and `extra: <rel>`. `<rel>` is relative to `.github/` with `/` separators.

- [ ] **Step 1: Branch, Pester 5, and commit the planning files**

```powershell
git switch master; git pull --ff-only; git switch -c chore/HARNESS-NATIVE-001-ai-native-harness
powershell -NoProfile -Command "Install-Module Pester -MinimumVersion 5.5.0 -Scope CurrentUser -Force -SkipPublisherCheck"
git add specs/ai-native-harness .agent/tickets/HARNESS-NATIVE-001
git commit -m "chore(harness): open HARNESS-NATIVE-001 with spec, design and plan"
```

Expected: `powershell -NoProfile -Command "(Get-Module -ListAvailable Pester | Sort Version -Desc)[0].Version"` prints `5.x`.

- [ ] **Step 2: Write the failing tests**

```powershell
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
```

- [ ] **Step 3: Run the tests and confirm they fail**

Run: `powershell -NoProfile -Command "Invoke-Pester -Path tests/harness -Output Detailed"`
Expected: FAIL, because `AgentHarness.psm1` doesn't exist.

- [ ] **Step 4: Implement the Produces functions in `scripts/harness/AgentHarness.psm1`**

Lift `Split-Frontmatter` from the current `scripts/sync-agent-harness.ps1` and parse the rest line by line. Drift generates into a fresh directory under `[IO.Path]::GetTempPath()`, compares relative path sets, then compares text after `-replace "\r\n", "\n"`, and removes the temp directory in `finally`. Write files with `New-Object System.Text.UTF8Encoding($false)`.

- [ ] **Step 5: Run the tests and confirm they pass**

Run: `powershell -NoProfile -Command "Invoke-Pester -Path tests/harness -Output Detailed"`
Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add scripts/harness/AgentHarness.psm1 tests/harness/AgentHarness.Tests.ps1
git commit -m "feat(harness): add module that mirrors .claude rules/agents for Copilot"
```

---

### Task 2: Harness lint core and context report

**ACs:** AC-1, AC-2, AC-5

**Files:**
- Modify: `scripts/harness/AgentHarness.psm1`, `tests/harness/AgentHarness.Tests.ps1`

**Interfaces:**
- Consumes: Task 1.
- Produces:
  - `Invoke-HarnessLint -RepoRoot <string> [-Category <string[]>]` → `[pscustomobject[]]` with `Category`, `File` (repo-relative, `/`) and `Message`. It dispatches through a module-level ordered map `$script:LintChecks` (category name → scriptblock taking `$RepoRoot`), so later tasks register `Facts`, `Specs` and `Evals`. This task registers `Budget`, `Rules`, `Skills`, `Agents`, `Links`, `Staleness` and `Drift`.
  - `Get-HarnessContextReport -RepoRoot <string>` → `[pscustomobject]` with `AlwaysOnBytes`, `ModelSkillCount`, `UserSkillCount` and `SkillListingChars`.
- Values:
  - **Always-on** = `CLAUDE.md` + every file it `@`-imports (an `@` token outside backticks, resolved relative to the importing file, depth ≤ 4) + every `.claude/rules/**/*.md` without `paths`.
  - **Model-invocable** = a `SKILL.md` without `disable-model-invocation: true`; **user-only** = the rest.
  - **Rules:** ≥ 1 `paths` entry, no entry containing whitespace, file ≤ 4,096 B.
  - **Skills:**
    - name = folder; description starts `Use when` and is ≤ 300 chars; `SKILL.md` ≤ 6,144 B; ≤ 6 model-invocable.
    - The exact backticked token `` `<user-only-name>` `` must not appear in any other `SKILL.md`, in agents, `AGENTS.md` or `CLAUDE.md`. `` `/<name>` `` is allowed.
  - **Agents:** name = file; description present and ≤ 300 chars; body ≤ 3,072 B.
  - **Links:**
    - (a) Harness set: a backticked token starting with `.agent/`, `.claude/`, `.github/`, `src/`, `tests/`, `docs/`, `scripts/` or `specs/` is checked unless it contains any of `<>*{}$|` or whitespace. Strip a trailing `:<digits>` and `/`; the path must exist.
    - (b) Markdown-link set: port `check_links` from agentic-devkit `scripts/check_kit.py`. Match `!?\[[^\]]*\]\(([^)]*)\)` outside fences (` ``` ` or `~~~`) and outside inline code. Skip `http(s)://`, `mailto:`, `#…` and targets containing `<>{}$*`. Drop `#anchor` and `"title"`, URL-decode, then resolve relative to the file, or to the repo root if the target starts with `/`.
  - **Staleness** (harness set, case-sensitive substrings): `npm run`, `npm install`, `src/backend`, `Vitest`, `React Query`, `docs/tracking`, `docs/memory/`, `docs/plans/`, `docs/archive/`, `.agent/skills`, `.agent/agents`, `.agent/workflows`, `.agent/rules`, `.claude/commands`, `CODING_CONVENTIONS`, `API_PROJECT_STRUCTURE`, `MUI`, `DataGrid`, `onboard-analyzer`, `manage-documentation`, `analyze-po-requirement`, `analyze-codebase`.
  - **Drift:** one finding per `Get-HarnessDrift` line, with `File` = `.github/<rel>`.

- [ ] **Step 1: Write the failing tests** (append)

```powershell
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
    It 'flags a rule without paths' { Set-RepoFile '.claude/rules/backend.md' '# no front'; (Lint Rules).Count | Should -BeGreaterThan 0 }
    It 'flags prose in paths' {
        Set-RepoFile '.claude/rules/backend.md' "---`npaths: Only triggers for React files`n---`nB"
        (Lint Rules).Count | Should -BeGreaterThan 0
    }
    It 'flags a skill name that differs from its folder' {
        Set-RepoFile '.claude/skills/run-tests/SKILL.md' "---`nname: run-project-tests`ndescription: Use when running tests.`n---`nB"
        (Lint Skills).Count | Should -BeGreaterThan 0
    }
    It 'flags a description that is not trigger-only' {
        Set-RepoFile '.claude/skills/run-tests/SKILL.md' "---`nname: run-tests`ndescription: Runs the tests.`n---`nB"
        (Lint Skills).Count | Should -BeGreaterThan 0
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
        (Lint Agents).Count | Should -BeGreaterThan 0
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
    It 'flags stale terms' {
        Set-RepoFile '.claude/skills/run-tests/SKILL.md' "---`nname: run-tests`ndescription: Use when testing.`n---`nThen npm run dev."
        (Lint Staleness).Count | Should -Be 1
    }
    It 'flags drift' {
        Add-Content (Join-Path $repo '.github/agents/code-reviewer.agent.md') 'edit'
        (Lint Drift).File | Should -Contain '.github/agents/code-reviewer.agent.md'
    }
    It 'filters by category' { Set-RepoFile 'AGENTS.md' ('x' * 4097); (Lint Rules).Count | Should -Be 0 }
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
```

- [ ] **Step 2: Run the tests and confirm they fail**

Run: `powershell -NoProfile -Command "Invoke-Pester -Path tests/harness -Output Detailed"`
Expected: the Task 1 tests pass; the new ones fail with `The term 'Invoke-HarnessLint' is not recognized`.

- [ ] **Step 3: Implement `Invoke-HarnessLint`, the seven category checks and `Get-HarnessContextReport`**

Write one private `Test-<Category>` function per category and register each in `$script:LintChecks`. Use `(Get-Item $f).Length` for byte sizes. Export the two public functions.

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `powershell -NoProfile -Command "Invoke-Pester -Path tests/harness -Output Detailed"`
Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add scripts/harness/AgentHarness.psm1 tests/harness/AgentHarness.Tests.ps1
git commit -m "feat(harness): lint budgets, rules, skills, agents, links, staleness and drift"
```

---

### Task 3: Fact-check lint for AI-written content

**ACs:** AC-6

**Files:**
- Modify: `scripts/harness/AgentHarness.psm1`, `tests/harness/AgentHarness.Tests.ps1`
- Create: `scripts/harness/facts-allowlist.txt`

**Interfaces:**
- Consumes: `$script:LintChecks` (Task 2).
- Produces: the `Facts` category.
  - **Tokens:** backticked spans in the harness set, outside fenced blocks. Strip a trailing `(...)` or `()` and a trailing `<...>`.
  - **Candidate** if it matches `^(@[a-z0-9-]+/[a-z0-9.-]+|[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*)$`, has length ≥ 4, and (contains `[A-Z]` or starts with `use[A-Z]` or `@`).
  - **Not a candidate:** it's all-caps, or its last dotted segment is ≤ 5 lowercase chars (a file name).
  - **Corpus:** files `*.cs`, `*.csproj`, `*.cshtml`, `*.razor`, `*.ts`, `*.tsx`, `*.json`, `*.props` and `*.yml` under `src/` and `tests/`, plus root `*.sln`, `*.props` and `docker-compose*.yml`.
    - Walk directories by hand, never descending into `bin`, `obj`, `node_modules`, `dist`, `.git` or `tests/skill-evals`. Do not filter after `Get-ChildItem -Recurse`, which is too slow inside `node_modules`.
    - Load the corpus once per lint run.
  - **Match:** case-insensitive substring (`IndexOf(..., OrdinalIgnoreCase)`).
  - **Allowlist:** `scripts/harness/facts-allowlist.txt`, one `Token  # reason` per line, with `#` comment lines and blank lines allowed. A line without `# <reason>` is itself a finding.
  - **Finding:** `File` = the harness file; `Message` = `` `<token>` not found in source corpus ``.

- [ ] **Step 1: Write the failing tests** (append)

```powershell
Describe 'Facts lint' {
    BeforeEach {
        $repo = New-Repo @{
            'src/Shared/Errors.cs'                       = 'public class DataVerificationException : Exception {} public record ResultModel<T>(T Data);'
            'src/UI/mango-ui/src/hooks/useFetch.ts'      = 'export function useFetch() {}'
            'src/UI/mango-ui/node_modules/x/index.js'    = 'GhostService'
            'Directory.Packages.props'                   = '<PackageVersion Include="Shouldly" />'
            'scripts/harness/facts-allowlist.txt'        = "GetWidgetById  # illustrative name in naming examples`n"
        }
        function Facts($agents) { [IO.File]::WriteAllText((Join-Path $repo 'AGENTS.md'), $agents); @(Invoke-HarnessLint -RepoRoot $repo -Category Facts) }
    }
    It 'accepts symbols, generics, calls and packages found in the corpus' {
        (Facts 'Use `DataVerificationException`, `ResultModel<T>`, `useFetch()` and `Shouldly`.').Count | Should -Be 0
    }
    It 'flags a symbol that exists only in node_modules' { (Facts 'Call `GhostService`.').Message | Should -Match 'GhostService' }
    It 'skips file names, lowercase words, acronyms and fenced code' {
        (Facts "Read ``AGENTS.md`` and ``Directory.Packages.props``; run ``pnpm``; ``CRLF``.`n~~~`n``GhostService```n~~~").Count | Should -Be 0
    }
    It 'honours the allowlist' { (Facts 'e.g. `GetWidgetById`').Count | Should -Be 0 }
    It 'flags an allowlist entry without a reason' {
        [IO.File]::WriteAllText((Join-Path $repo 'scripts/harness/facts-allowlist.txt'), "GetWidgetById`n")
        (Facts '# ok').File | Should -Contain 'scripts/harness/facts-allowlist.txt'
    }
    It 'does not scan specs' {
        New-Item -ItemType Directory -Force (Join-Path $repo 'specs/x') | Out-Null
        [IO.File]::WriteAllText((Join-Path $repo 'specs/x/spec.md'), 'Add `CouponExpiryJob`.')
        (Facts '# ok').Count | Should -Be 0
    }
}
```

- [ ] **Step 2: Run the tests and confirm the new ones fail**

Run: `powershell -NoProfile -Command "Invoke-Pester -Path tests/harness -Output Detailed"`
Expected: the 'Facts lint' tests fail, and 'reports nothing for a valid repo' still passes.

- [ ] **Step 3: Implement the `Facts` check and register it; create `facts-allowlist.txt`**

The allowlist starts with a header comment only.

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `powershell -NoProfile -Command "Invoke-Pester -Path tests/harness -Output Detailed"`
Expected: all tests pass.

- [ ] **Step 5: Record a baseline on the real repo**

Run: `powershell -NoProfile -Command "Import-Module ./scripts/harness/AgentHarness.psm1; Invoke-HarnessLint -RepoRoot . -Category Facts | Format-Table -AutoSize | Out-String -Width 200"`
Expected: findings that include `Coupon.API` (`.agent/memory/architecture.md`) and `MediatR` (`AGENTS.md`), if backticked there. Paste the list into the ticket's `## Session Log` as the baseline; Tasks 6–8 and 12 burn it down. Time the run; it must take ≤ 60 s.

- [ ] **Step 6: Commit**

```bash
git add scripts/harness tests/harness
git commit -m "feat(harness): fact-check backticked symbols in AI-written guidance against the code"
```

---

### Task 4: Spec layer from agentic-devkit

**ACs:** AC-7

**Files:**
- Create: `specs/_template/spec.md`, `specs/_template/acceptance-criteria.md`, `specs/_template/test-plan.md`
- Modify: `scripts/harness/AgentHarness.psm1`, `tests/harness/AgentHarness.Tests.ps1`

**Interfaces:**
- Produces:
  - The templates, ported from `D:\Learning\agentic-devkit\specs\_template\` with the adaptations in `design.md` §8:
    - `Related` links point to `../../.agent/memory/index.md` and `../../docs/ARCHITECTURE.md`.
    - The test-plan table header is `| AC | Unit (xUnit) | Integration | Lint/Build | Manual |`.
    - The devkit `tests/cases/` scenario paragraph is replaced by an inline `## Scenarios` list.
  - The `Specs` lint category, applied to each `specs/<slug>/` where slug ≠ `_template`:
    - `spec.md`, `acceptance-criteria.md` and `test-plan.md` exist.
    - `spec.md` has a line `^\*\*Status:\*\*\s*(draft|in-review|approved|shipped)\s*$`.
    - The AC ids are the matches of `^- \[[ x]\] (AC-\d+):` in `acceptance-criteria.md`: at least 1, all unique.
    - Every AC id appears in a `|`-row of `test-plan.md`, and every `AC-\d+` in a `|`-row is defined.
    - `specs/_template/` must hold the three files. This is checked only when `specs/` exists, so the Task 2 'valid repo' fixture stays clean.

- [ ] **Step 1: Write the failing tests** (append)

```powershell
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
    It 'accepts a complete spec' { (Specs).Count | Should -Be 0 }
    It 'flags an invalid status' { Set-Spec 'spec.md' '**Status:** done'; (Specs).Count | Should -Be 1 }
    It 'flags duplicate AC ids' { Set-Spec 'acceptance-criteria.md' "- [ ] AC-1: x`n- [ ] AC-1: y`n"; (Specs).Message -join ' ' | Should -Match 'AC-1' }
    It 'flags an AC missing from the test plan' { Set-Spec 'test-plan.md' "| AC-1 | x |`n"; (Specs).Message -join ' ' | Should -Match 'AC-2' }
    It 'flags an undefined AC in the test plan' { Set-Spec 'test-plan.md' "| AC-1 | x |`n| AC-2 | x |`n| AC-3 | x |`n"; (Specs).Message -join ' ' | Should -Match 'AC-3' }
    It 'flags a missing spec file' { Remove-Item (Join-Path $repo 'specs/a/test-plan.md'); (Specs).Count | Should -BeGreaterThan 0 }
}
```

- [ ] **Step 2: Run the tests and confirm the new ones fail**

Run: `powershell -NoProfile -Command "Invoke-Pester -Path tests/harness -Output Detailed"`
Expected: the 'Specs lint' tests fail, and all earlier tests pass.

- [ ] **Step 3: Implement and register `Specs`; write the three templates**
- [ ] **Step 4: Run the tests and lint this feature's own spec**

Run: `powershell -NoProfile -Command "Invoke-Pester -Path tests/harness -Output Detailed"`
Expected: all tests pass.

Run: `powershell -NoProfile -Command "Import-Module ./scripts/harness/AgentHarness.psm1; Invoke-HarnessLint -RepoRoot . -Category Specs,Links | Where-Object File -like 'specs/*'"`
Expected: no output, so `specs/ai-native-harness/` and `specs/_template/` are valid.

- [ ] **Step 5: Commit**

```bash
git add specs/_template scripts/harness tests/harness
git commit -m "feat(harness): adopt agentic-devkit spec templates with AC traceability lint"
```

---

### Task 5: Make `.claude/` canonical (mechanical move)

**ACs:** AC-11

**Files:**
- Delete (generated): `.claude/skills/**`, `.claude/agents/**`, `.claude/commands/**`
- Move: `.agent/skills/<n>/` → `.claude/skills/<n>/` (8); `.agent/agents/<n>.agent.md` → `.claude/agents/<n>.md` (3); `.agent/workflows/<n>.md` → `.claude/skills/<n>/SKILL.md` (7)
- Rewrite: `scripts/sync-agent-harness.ps1`
- Create (generated): `.github/agents/*.agent.md`

**Interfaces:**
- Consumes: `Invoke-HarnessSync`, `Invoke-HarnessLint`, `Get-HarnessContextReport`.
- Produces `scripts/sync-agent-harness.ps1` with `[switch]$Check` and `[string[]]$Category`:
  - No switch: sync, print the context report, exit 0.
  - `-Check`: lint, print `[<Category>] <File>: <Message>` per finding, exit 1 if there are any findings, else 0.
  - `$Category` is split on `,`, because `powershell -File` passes `Drift,Agents` as one string.

- [ ] **Step 1: Swap the generated tree for the sources**

```powershell
git rm -r -q .claude/skills .claude/agents .claude/commands
if (Test-Path .claude/skills) { throw '.claude/skills still exists; git mv would nest the move inside it' }
git mv .agent/skills .claude/skills
New-Item -ItemType Directory -Force .claude/agents | Out-Null
foreach ($n in 'code-reviewer','codebase-analyzer','unit-test-creator') { git mv ".agent/agents/$n.agent.md" ".claude/agents/$n.md" }
foreach ($n in 'analyze-requirement','fix-warnings','implement-ticket','onboarding','review','test-gen','update-docs') {
    New-Item -ItemType Directory -Force ".claude/skills/$n" | Out-Null
    git mv ".agent/workflows/$n.md" ".claude/skills/$n/SKILL.md"
}
```

- [ ] **Step 2: Fix the frontmatter of the moved files**

- In agents, delete `argument-hint:` and the `tools:` block (they're Copilot tool names).
- In the 7 former workflows, set `name: <n>`, keep `description:`, and add `disable-model-invocation: true`.

- [ ] **Step 3: Rewrite `scripts/sync-agent-harness.ps1` as `Import-Module (Join-Path $PSScriptRoot 'harness/AgentHarness.psm1') -Force` plus the switch handling**

Delete the old generation code.

- [ ] **Step 4: Sync and verify**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync-agent-harness.ps1`
Expected: 3 files in `.github/agents/`, and the report shows `UserSkillCount: 7`.

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync-agent-harness.ps1 -Check -Category Drift,Agents`
Expected: exit 0.

Run: `git ls-files .agent/skills .agent/agents .agent/workflows .claude/commands`
Expected: empty.

- [ ] **Step 5: Commit**

```bash
git add -A .agent .claude .github/agents scripts/sync-agent-harness.ps1
git commit -m "refactor(harness): make .claude/ canonical and mirror agents to .github"
```

---

### Task 6: Consolidate skills and agents; make workflows spec-aware

**ACs:** AC-2, AC-8, AC-11

**Files:**
- Modify: `.claude/skills/{code-review,create-unit-test,analyze-requirement,onboarding,update-docs,implement-ticket,fix-warnings}/SKILL.md`
- Delete: `.claude/skills/{review,test-gen,analyze-po-requirement,onboard-analyzer,manage-documentation,analyze-codebase}/`
- Create: `.claude/skills/add-feature-slice/SKILL.md`, plus `references/{feature-slice,program-wiring,integration-events}.md`
- Modify: `.claude/agents/*.md`, `.agent/tickets/_templates/feature.md`
- Regenerate: `.github/agents/*.agent.md`

**Interfaces:**
- Consumes: `specs/_template/` (Task 4).
- Produces the final skill set. Descriptions are exact:

| Skill | `disable-model-invocation` | `description` |
|---|---|---|
| `code-review` | — | Use when reviewing new code before merging, auditing existing code for vulnerabilities, or checking performance and idiomatic-usage concerns in this repo's C#/.NET or React code. |
| `create-unit-test` | — | Use when a C# class or method needs unit test coverage and the tests must follow this repo's exact xUnit/Moq/Shouldly naming and placement conventions. |
| `run-tests` | — | *(unchanged)* |
| `generate-commit` | — | *(unchanged)* |
| `add-feature-slice` | — | Use when adding a new API project, feature slice, minimal-API route group, or integration event under src/Services and the exact Vertical Slice file layout and wiring templates are needed. |
| `implement-ticket` | `true` | Use when the user runs /implement-ticket to carry a ticket from spec and plan through implementation, AC verification and documentation. |
| `analyze-requirement` | `true` | Use when the user runs /analyze-requirement to turn a PO requirement into a spec under specs/<slug>/ with testable acceptance criteria and a test plan. |
| `onboarding` | `true` | Use when the user runs /onboarding to generate or refresh the onboarding pack under /docs and get a Day-1 guide. |
| `update-docs` | `true` | Use when the user runs /update-docs to bring /docs back in sync with code that just landed. |
| `fix-warnings` | `true` | Use when the user runs /fix-warnings to clear .NET build warnings (WarningsAsErrors is on) with modern C# 15 fixes. |

- [ ] **Step 1: Merge the five pairs**

The target keeps the frontmatter pinned above. Its body becomes the target body plus the source's uncovered steps. Then `git rm -r` the source.

| Target | Source |
|---|---|
| `code-review` | `review` |
| `create-unit-test` | `test-gen` |
| `analyze-requirement` | `analyze-po-requirement` |
| `onboarding` | `onboard-analyzer` |
| `update-docs` | `manage-documentation` |

Replace "C# 14 fixes" with "C# 15 fixes", but keep "C# 14 extension blocks".

- [ ] **Step 2: Make the workflows spec-aware**

- **`analyze-requirement`:** write `specs/<slug>/spec.md`, `acceptance-criteria.md` and `test-plan.md` from `specs/_template/`, with Status `in-review`. Every AC is `- [ ] AC-n: Given … when … then …` and appears in the test-plan table. Unresolved points go under Open questions and are never invented as ACs.
- **`implement-ticket`:**
  - Step 1 for `feature` tickets: read and follow `.claude/skills/analyze-requirement/SKILL.md`, then put `specs/<slug>/` under the ticket's `## Links` → Docs. Approval sets the spec Status to `approved`.
  - Step 3: tick an AC `[x]` only with cited evidence (test name, command plus output line); list unmet ACs and stop.
  - Step 4: set the spec Status to `shipped`.
  - Reference `fix-warnings` and `update-docs` by `.claude/skills/<n>/SKILL.md`.
- **`.agent/tickets/_templates/feature.md`:** replace the `### Requirement` body with "Spec: `specs/<slug>/` — problem, acceptance criteria and test plan live there." Change the step task "Run the analyze-requirement sub-workflow" to "Write `specs/<slug>/` (analyze-requirement)".

- [ ] **Step 3: Create `add-feature-slice` from `.agent/API_PROJECT_STRUCTURE.md`**

`SKILL.md` is ≤ 2,048 B: when to use it, a 5-step checklist (slice → validator → route → wiring → test), and links to each reference. Move sections by heading:

| Heading | Destination |
|---|---|
| "Directory Structure", "Feature Slice Template", "Route Mapping" | `references/feature-slice.md` |
| "Ultra-Clean Program.cs", "Dependency Injection & Middleware Configuration" | `references/program-wiring.md` |
| "Event-Driven Architecture" | `references/integration-events.md` |

- [ ] **Step 4: Thin the agents**

Each body is ≤ 1,536 B (`codebase-analyzer` ≤ 3,072 B) and contains three things: the role, the line `` Follow `.claude/skills/<skill>/SKILL.md` ``, and the output format. Skill mapping: `code-reviewer` → `code-review`; `unit-test-creator` → `create-unit-test`.

`codebase-analyzer` absorbs the body of `.claude/skills/analyze-codebase/SKILL.md`; then `git rm -r .claude/skills/analyze-codebase`.

Descriptions are exact:
- `code-reviewer`: Use for an isolated, fresh-context review of C#/.NET or React changes in this repo; returns findings ranked by severity.
- `codebase-analyzer`: Use to map the projects, dependencies and tech stack of an unfamiliar area of this .NET solution before a cross-cutting change, without filling the main context.
- `unit-test-creator`: Use to generate xUnit + Moq + Shouldly tests for a C# class in an isolated context, following this repo's naming and placement conventions.

- [ ] **Step 5: Sync and lint**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync-agent-harness.ps1`
Expected: `ModelSkillCount: 5`, `UserSkillCount: 5`.

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync-agent-harness.ps1 -Check -Category Skills,Agents,Drift,Facts`
Expected: exit 0. Fix Facts findings in `.claude/**` at the source, or allowlist them with a reason.

- [ ] **Step 6: Commit**

```bash
git add -A .claude .github/agents .agent/tickets/_templates/feature.md scripts/harness/facts-allowlist.txt
git commit -m "refactor(harness): merge duplicate skills, add add-feature-slice, spec-aware workflows"
```

---

### Task 7: Path-scoped rules

**ACs:** AC-3, AC-11

**Files:**
- Create: `.claude/rules/{backend,backend-testing,frontend,docs,specs}.md` and `.claude/rules/memory/{identity,checkout-saga,ui-web,cart-checkout}.md`
- Delete: `.agent/rules/`, `.agent/CODING_CONVENTIONS.md`, `.agent/API_PROJECT_STRUCTURE.md`
- Create (generated): `.github/instructions/**`

**Interfaces:**
- Consumes: `add-feature-slice` (Task 6), which holds the templates.
- Produces the rules below. `paths` is a YAML block list of double-quoted globs, with no braces.

| Rule | `paths` | Must contain |
|---|---|---|
| `backend.md` | `src/**/*.cs` | Every `CODING_CONVENTIONS.md` section as one-line bullets; architecture "Core Principles" (VSA, no repository except `Mango.Orchestrators`, no cross-slice calls, thin `Program.cs`, Logging→Validation→Tx); "Templates: the `add-feature-slice` skill" |
| `backend-testing.md` | `tests/**/*.cs` | The body of `.agent/rules/backend-testing.md` |
| `frontend.md` | `src/UI/mango-ui/src/**/*.ts`, `src/UI/mango-ui/src/**/*.tsx` | TS strict, no `any`, `import type`, `@/` alias; API calls in `src/api/` consumed via hooks in `src/hooks/` (`useFetch`); reuse contexts before adding globals; i18n EN+VI via react-i18next; lint + build, since there is no test runner; background in `.agent/memory/domains/frontend-spa.md`. **No MUI or DataGrid.** |
| `docs.md` | `docs/**/*.md` | The body of `.agent/rules/documentation-standards.md` |
| `specs.md` | `specs/**/*.md` | ACs are `- [ ] AC-n: Given … when … then …`, observable and testable; never renumber an approved AC (append instead); Status lifecycle `draft → in-review → approved → shipped`; the test plan maps every AC; ticks need cited evidence |
| `memory/identity.md` | `src/Services/Identity.API/**`, `src/Services/OpenIdentity.App/**`, `src/Mango.AppHost/**` | `` Before changing these files, read `.agent/memory/domains/identity.md`. `` |
| `memory/checkout-saga.md` | `src/Services/Mango.Orchestrators/**`, `src/Services/Orders.API/**`, `src/Services/Payments.API/**` | the same, pointing at `checkout-saga.md` |
| `memory/ui-web.md` | `src/UI/Mango.Web/**/*.cshtml`, `src/UI/Mango.Web/**/*.css`, `src/Services/Identity.API/**/*.cshtml` | the same, pointing at `ui-web.md` |
| `memory/cart-checkout.md` | `src/UI/Mango.Web/Controllers/**`, `src/Services/ShoppingCart.API/**`, `src/Services/Coupons.API/**` | the same, pointing at `cart-checkout.md` |

- [ ] **Step 1: Write the nine rule files**
- [ ] **Step 2: Delete the old sources**

```powershell
git rm -r -q .agent/rules .agent/CODING_CONVENTIONS.md .agent/API_PROJECT_STRUCTURE.md
```

- [ ] **Step 3: Sync and lint**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync-agent-harness.ps1`
Expected: 9 `*.instructions.md` files under `.github/instructions/`, 4 of them in `memory/`.

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync-agent-harness.ps1 -Check -Category Rules,Drift,Facts`
Expected: exit 0.

Run: `Select-String -Path .github/instructions/frontend.instructions.md -Pattern '^applyTo:'`
Expected: `applyTo: "src/UI/mango-ui/src/**/*.ts,src/UI/mango-ui/src/**/*.tsx"`.

- [ ] **Step 4: Commit**

```bash
git add -A .claude/rules .agent .github/instructions
git commit -m "refactor(harness): path-scoped rules for Claude and Copilot; drop duplicate conventions"
```

---

### Task 8: Lean always-on files

**ACs:** AC-1

**Files:**
- Rewrite: `AGENTS.md`, `CLAUDE.md`
- Modify: `.agent/memory/index.md`

**Interfaces:**
- Consumes: the skill names (Task 6), the rule names (Task 7), and `specs/_template/` (Task 4).
- Produces: always-on ≤ 5,120 B.

- [ ] **Step 1: Rewrite `AGENTS.md` (≤ 4,096 B) with these sections, in order**

1. **What this is** — two lines.
2. **Commands** — restore, build, AppHost, test (all / one project / `--filter "FullyQualifiedName~Class.Method"`), `dotnet format --verify-no-changes`, pnpm `--dir src/UI/mango-ui` lint/build/dev, and "there is no frontend test runner".
3. **Guardrails** — one line each:
   - VSA
   - no repository layer (except `Mango.Orchestrators`)
   - no cross-slice calls
   - thin `Program.cs` via `AddApiDefaults`/`UseApiPipeline`
   - `ResultModel<T>`
   - `DataVerificationException` plus the global handler
   - FluentValidation per command
   - Logging→Validation→Tx behaviours on the in-house mediator (name it as the code does; Facts checks it)
   - central package versions
   - pnpm
   - TS strict, no `any`
   - test names `Method_When_X_Then_Y`
   - `WarningsAsErrors`
4. **Where guidance lives** — rules auto-load from `.claude/rules/` (mirrored to `.github/instructions/`); skills in `.claude/skills/`; memory `.agent/memory/index.md`; done checklist `.agent/memory/ops/task-completion.md`.
5. **Specs and tickets** — feature work gets `specs/<slug>/` from `specs/_template/`, and ACs are ticked only with evidence. Then the ticket path, kind/template, resume rule, and skipped-task syntax.

- [ ] **Step 2: Replace `CLAUDE.md` with exactly this**

```markdown
# CLAUDE.md

@AGENTS.md

## Claude Code specifics

- Rules in `.claude/rules/` load automatically when you read or edit matching files; don't pre-read them.
- Serena MCP (`.mcp.json`, port 9121): start with `./scripts/start-mcp.ps1`, confirm with `/mcp`. Never use Serena's memory tools; memory lives in `.agent/memory/`.
- After editing `.claude/rules/` or `.claude/agents/`, run `./scripts/sync-agent-harness.ps1` to refresh the Copilot mirror in `.github/`.
```

- [ ] **Step 3: In `.agent/memory/index.md`, replace the "do not turn them into `@`-imports" sentence**

New text: "Nothing imports this file; `AGENTS.md` links it. Domain files also surface automatically through `.claude/rules/memory/`."

- [ ] **Step 4: Check the budget and record the numbers**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync-agent-harness.ps1 -Check -Category Budget,Facts,Links`
Expected: exit 0.

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync-agent-harness.ps1`
Expected: `AlwaysOnBytes` ≤ 5120. Copy the report line into the ticket's Session Log.

- [ ] **Step 5: Commit**

```bash
git add AGENTS.md CLAUDE.md .agent/memory/index.md
git commit -m "refactor(harness): slim AGENTS.md and CLAUDE.md to an always-on index"
```

---

### Task 9: Skill eval engine — eval sets, checks, grading, benchmark

**ACs:** AC-9

**Files:**
- Create: `scripts/harness/SkillEval.psm1`, `tests/harness/SkillEval.Tests.ps1`, `tests/skill-evals/_grader.md`
- Modify: `scripts/harness/AgentHarness.psm1` (register `Evals`), `tests/harness/AgentHarness.Tests.ps1`

**Interfaces:**
- Produces (exported from `SkillEval.psm1`):
  - `Read-SkillEvalSet -Path <string>` → the parsed object.
    - Throws if `skill_name` ≠ the parent folder name, ids are missing or duplicated, an eval lacks `name` or `prompt`, an eval has neither `checks` nor `expectations`, or a check `type` isn't one of `command|file-matches|no-edits|no-commit|response-matches`.
    - The schema is the devkit `evals.json` plus `name`, `setup` (`string[]`) and `checks` (`object[]`).
  - `Get-ClaudeArgs -Purpose run|grade [-Model <string>] [-MaxTurns <int>=30]` → `[string[]]`:
    - **run:** `-p --output-format json --max-turns <n> --permission-mode acceptEdits --allowedTools "Read,Grep,Glob,Edit,Write,Bash(dotnet build:*),Bash(dotnet test:*),Bash(git diff:*),Bash(git status:*),Bash(git log:*)"`
    - **grade:** `-p --output-format json --max-turns 1`
    - Both add `--model <m>` only when `-Model` is given.
  - `Invoke-EvalChecks -WorkDir <string> -ResponseText <string> -Checks <object[]> [-SetupTree <string>] [-BaseCommit <string>]` → `[pscustomobject[]]` of `text`, `passed`, `evidence` and `source='check'`.
    - `command` runs `{run}` in WorkDir (via `powershell` or `pwsh -NoProfile -Command`) and passes on exit `expect_exit` (default 0).
    - `file-matches` passes if ≥ 1 file under `{glob}` matches `{pattern}`.
    - `no-edits` passes if, after `git add -A`, `git write-tree` equals `SetupTree`.
    - `no-commit` passes if `git rev-parse HEAD` equals `BaseCommit`.
    - `response-matches` applies a regex to the response.
  - `Invoke-EvalGrade -RunDir <string> -Expectations <string[]> -Invoker <scriptblock> -GraderPromptPath <string> [-Model]` → writes and returns `grading.json`: `{ expectations[{text, passed, evidence, source}], pass_rate, cost_usd, duration_ms }`.
    - It reads `outputs/response.md`, `outputs/diff.patch` and `checks.json`.
    - It calls `& $Invoker -WorkDir $RunDir -Prompt <grader prompt + expectations + outputs> -ClaudeArgs (Get-ClaudeArgs -Purpose grade)` only when there are expectations, and parses `result` as JSON (taking the first `{` to the last `}`).
    - Unparseable output marks every grader expectation `passed=false` with evidence `grader returned unparseable output`.
    - `cost_usd` and `duration_ms` add up the run (from `outputs/response.json`) and the grader call.
  - `Get-EvalBenchmark -ResultsDir <string>` → per-skill objects `{skill, evals, with_skill_pass_rate, without_skill_pass_rate, delta, verdict, cost_usd, duration_s, non_discriminating[]}`. It also writes `benchmark.json` and `summary.md` (`| Skill | Evals | With skill | Without | Δ | Verdict | Cost (USD) | Time (s) |`).
    - `verdict` is `helps` if Δ ≥ 0.10, `hurts` if Δ ≤ −0.10, else `neutral`.
    - `non_discriminating` lists expectation texts that passed in every run of both configs.
  - The `Evals` lint category (in `AgentHarness.psm1`, which imports `SkillEval.psm1`): one finding per `tests/skill-evals/<dir>/evals.json` for which `Read-SkillEvalSet` throws or `.claude/skills/<dir>/SKILL.md` doesn't exist. Directories starting with `_` are skipped.
- `tests/skill-evals/_grader.md`: adapted from `D:\Learning\agentic-devkit\.claude\skills\skill-creator\agents\grader.md`, keeping an Apache-2.0 attribution line, ≤ 3 KB. It judges each expectation only from the given evidence, quotes the evidence, and replies with JSON only: `{"expectations":[{"text":…,"passed":…,"evidence":…}]}`.

- [ ] **Step 1: Write the failing tests in `tests/harness/SkillEval.Tests.ps1`**

```powershell
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
}

Describe 'Invoke-EvalChecks' {
    BeforeEach { $wd = New-Dir @{ 'tests/X.Tests/FooTests.cs' = 'public void Get_When_Missing_Then_NotFound() {}' } }
    It 'passes file-matches and response-matches' {
        $r = Invoke-EvalChecks -WorkDir $wd -ResponseText 'fix(products): guard null' -Checks @(
            @{ type = 'file-matches'; glob = 'tests/**/*Tests.cs'; pattern = '_When_\w+_Then_' },
            @{ type = 'response-matches'; pattern = '^fix\(' })
        $r.passed | Should -Be @($true, $true)
    }
    It 'fails a command with non-zero exit and records evidence' {
        $r = Invoke-EvalChecks -WorkDir $wd -ResponseText '' -Checks @(@{ type = 'command'; run = 'exit 3' })
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
```

Append to `tests/harness/AgentHarness.Tests.ps1`:

```powershell
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
```

- [ ] **Step 2: Run the tests and confirm they fail**

Run: `powershell -NoProfile -Command "Invoke-Pester -Path tests/harness -Output Detailed"`
Expected: the SkillEval and 'Evals lint' tests fail; all earlier tests pass.

- [ ] **Step 3: Implement `SkillEval.psm1`, register `Evals`, and write `_grader.md`**

`ConvertFrom-Json` returns PSCustomObjects; read check fields with `$c.type` and `$c.pattern`. Round `pass_rate` and `delta` to 2 decimals and `cost_usd` to 4, so the tests' exact comparisons hold.

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `powershell -NoProfile -Command "Invoke-Pester -Path tests/harness -Output Detailed"`
Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add scripts/harness tests/harness tests/skill-evals/_grader.md
git commit -m "feat(harness): skill eval engine with deterministic checks, LLM grading and benchmark"
```

---

### Task 10: Skill eval runner — isolated worktrees and CLI entry point

**ACs:** AC-9 (plus the Safety and Cost NFRs)

**Files:**
- Modify: `scripts/harness/SkillEval.psm1`, `tests/harness/SkillEval.Tests.ps1`
- Create: `scripts/eval-skills.ps1`
- Modify: `.gitignore` (add `tests/skill-evals/_results/`)

**Interfaces:**
- Consumes: Task 9 functions.
- Produces:
  - `Invoke-ClaudeCli -WorkDir -Prompt -ClaudeArgs` is the default invoker. It sets `$OutputEncoding` and `[Console]::OutputEncoding` to UTF-8 without BOM, pipes `$Prompt` on stdin into `claude @ClaudeArgs` from inside WorkDir, and returns stdout as one string. Stdin avoids PowerShell 5.1 quote mangling of native arguments.
  - `Invoke-EvalRun -RepoRoot -SkillName -Eval -Config with_skill|without_skill -RunIndex -OutDir -Invoker [-Model] [-MaxTurns] [-Ref HEAD]` runs these steps in order:
    1. `git worktree add --detach <temp>/mango-eval-<guid> <Ref>`.
    2. `without_skill`: delete `<wt>/.claude/skills/<SkillName>`.
    3. Run each `setup` command in the worktree.
    4. `git add -A`; record `SetupTree` (`git write-tree`) and `BaseCommit` (`git rev-parse HEAD`).
    5. Prompt is `/<SkillName> <prompt>` for `with_skill`, or the plain `<prompt>` otherwise.
    6. Invoke.
    7. Write `outputs/response.json` (raw), `outputs/response.md` (`.result`), `outputs/diff.patch` (`git add -A; git diff --cached <SetupTree>`) and `checks.json`.
    8. In `finally`: `git worktree remove --force` and `git worktree prune`.
    - If the invoker throws, the runner writes `error.txt`, records every check as failed, and returns normally.
  - `Invoke-SkillEvals -RepoRoot -Skill <string[]> [-EvalId <int[]>] [-Runs 1] [-Model] [-MaxTurns 30] [-Invoker] [-DryRun]` → the results directory `tests/skill-evals/_results/<yyyyMMdd-HHmmss>/`.
    - Layout: `<skill>/eval-<id>-<name>/<config>/run-<n>/`.
    - It warns when `git status --porcelain -- .claude/skills tests/skill-evals` isn't empty, because evals run against committed `Ref`. The check is skipped under `-DryRun`, which needs no git repo.
    - It runs `Invoke-EvalRun` and then `Invoke-EvalGrade` for every eval × config × run, then `Get-EvalBenchmark`.
    - `-DryRun` prints one line per planned run, `<skill> eval-<id>-<name> <config> run-<n>: claude <args>`, invokes nothing, and returns `$null`.
  - `scripts/eval-skills.ps1` passes its parameters to `Invoke-SkillEvals` (splitting `-Skill` and `-EvalId` on `,`) and prints `summary.md`.

- [ ] **Step 1: Write the failing tests** (append to `SkillEval.Tests.ps1`)

```powershell
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

Describe 'Invoke-SkillEvals -DryRun' {
    It 'lists every planned run and invokes nothing' {
        $repo = New-Dir @{ '.claude/skills/run-tests/SKILL.md' = "---`nname: run-tests`ndescription: Use when testing.`n---`nB"; 'tests/skill-evals/run-tests/evals.json' = $valid }
        $lines = Invoke-SkillEvals -RepoRoot $repo -Skill run-tests -Runs 2 -Invoker { throw 'no' } -DryRun 6>&1
        @($lines | Where-Object { "$_" -match 'claude -p' }).Count | Should -Be 4
    }
}
```

- [ ] **Step 2: Run the tests and confirm they fail**

Run: `powershell -NoProfile -Command "Invoke-Pester -Path tests/harness -Output Detailed"`
Expected: the new tests fail with `Invoke-EvalRun` not recognized.

- [ ] **Step 3: Implement the Produces items; add the `.gitignore` line**
- [ ] **Step 4: Run the tests and confirm they pass**

Run: `powershell -NoProfile -Command "Invoke-Pester -Path tests/harness -Output Detailed"`
Expected: all tests pass, and `git worktree list` in the real repo shows only the main tree.

- [ ] **Step 5: Commit**

```bash
git add scripts/harness/SkillEval.psm1 scripts/eval-skills.ps1 tests/harness/SkillEval.Tests.ps1 .gitignore
git commit -m "feat(harness): run skill evals with and without the skill in disposable worktrees"
```

---

### Task 11: Author evals for six skills and run the first benchmark

**ACs:** AC-10 (and AC-8 via the `analyze-requirement` evals)

**Files:**
- Create: `tests/skill-evals/{code-review,create-unit-test,run-tests,generate-commit,add-feature-slice,analyze-requirement}/evals.json`
- Create: `tests/skill-evals/<skill>/files/*.patch` (fixture patches, made against current `master` code)

**Interfaces:**
- Consumes: the `evals.json` schema (Task 9) and `scripts/eval-skills.ps1` (Task 10).
- Produces 18 evals: ids 1–3 per skill, with names and minimum checks as below.
  - Every eval also lists 2–4 `expectations` restating `expected_output` as verifiable sentences.
  - Fixture patches touch real files. For example, pick an existing Products.API handler without tests for `create-unit-test`.
  - Patches must apply cleanly to `HEAD` (`git apply --check`).

| Skill | Evals (id name — setup) | Minimum checks |
|---|---|---|
| `code-review` | 1 `flags-repository-layer` — patch adds a `ProductRepository` used by a Products handler · 2 `flags-missing-validator` — patch adds a command without a validator · 3 `clean-change-no-blockers` — patch makes a correct small change | all: `no-edits`; 1: `response-matches` `(?i)repositor`; 2: `response-matches` `(?i)validator` |
| `create-unit-test` | 1 `products-query-handler` · 2 `cart-command-handler` · 3 `validator-rules` | `file-matches` (target test project, `_When_\w+_Then_`); `command` `dotnet test <proj> --filter <NewClass>` |
| `run-tests` | 1 `single-project-summary` · 2 `triage-broken-handler` — patch breaks a handler · 3 `single-method-filter` | 1, 3: `no-edits`; 2: `response-matches` naming the broken file |
| `generate-commit` | 1 `fix-from-null-guard` · 2 `feat-from-new-endpoint` · 3 `docs-only-change` — each setup is `git apply` then `git add -A` | `no-commit`; `response-matches` `(?m)^fix\(` / `^feat\(` / `^docs\(` |
| `add-feature-slice` | 1 `query-by-category` · 2 `command-with-validator` · 3 `integration-event-handler` | `command` `dotnet build src/Services/Products.API/Products.API.csproj` (adjust the project per eval); `file-matches` for handler, validator and route |
| `analyze-requirement` | 1 `coupon-expiry` · 2 `order-cancellation-window` · 3 `ambiguous-requirement` | 1, 2: `command` `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync-agent-harness.ps1 -Check -Category Specs`; 3: expectation "lists open questions instead of inventing ACs for undecided points" |

- [ ] **Step 1: Write the six `evals.json` files and their patches; lint them**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync-agent-harness.ps1 -Check -Category Evals`
Expected: exit 0.

- [ ] **Step 2: Commit the evals (runs use committed `HEAD`)**

```bash
git add tests/skill-evals
git commit -m "test(harness): add output evals for six skills"
```

- [ ] **Step 3: Dry run**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/eval-skills.ps1 -Skill code-review,create-unit-test,run-tests,generate-commit,add-feature-slice,analyze-requirement -DryRun`
Expected: 36 planned runs.

- [ ] **Step 4: Smoke run one eval**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/eval-skills.ps1 -Skill generate-commit -EvalId 1`
Expected: in `_results/<ts>/generate-commit/eval-1-fix-from-null-guard/`, both configs have a `response.json` with a non-empty `result` and `total_cost_usd`, and both have a `grading.json`.

If the session was denied a tool because Claude Code on Windows used its PowerShell tool, add the matching `PowerShell(dotnet build:*)`-style entries to the `run` arguments in `Get-ClaudeArgs`, update its test, and commit. `git worktree list` must show only the main tree.

- [ ] **Step 5: Full run and record the results**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/eval-skills.ps1 -Skill code-review,create-unit-test,run-tests,generate-commit,add-feature-slice,analyze-requirement`
Expected: `summary.md` with 6 rows.

Paste the table into the ticket's Session Log. For every skill whose verdict isn't `helps`, add an `## Open Questions` item naming the skill and its failing or non-discriminating expectations. Then tick AC-10.

---

### Task 12: Docs, CI, close-out

**ACs:** AC-5, AC-12, plus the Portability and Performance NFRs

**Files:**
- Rewrite: `.agent/README.md`, `.agent/tools/harness.md`
- Create: `.agent/tools/skill-evals.md` (how to write `evals.json`, run, read `summary.md`, cost guidance)
- Modify: `.agent/tools/scripts.md`, `.agent/memory/architecture.md` (harness bullets; fix the `Coupon.API` claim), `.agent/memory/ops/commands.md`, `.agent/memory/ops/task-completion.md`, `.agent/memory/MEMORY_GUIDE.md` (if it describes the index `@`-import), `README.md` (harness section), `scripts/bootstrap.ps1` (the harness message)
- Modify: `.claude/settings.json` — add `"Bash(powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync-agent-harness.ps1:*)"` to `permissions.allow`
- Modify: `.github/workflows/build.yml`, `.agent/tickets/HARNESS-NATIVE-001/ticket.md`, `specs/ai-native-harness/{spec,acceptance-criteria}.md`

- [ ] **Step 1: Run the full lint to get the remaining to-do list**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync-agent-harness.ps1 -Check`
Expected: findings only in `Links`, `Staleness` and `Facts` for the files above.

- [ ] **Step 2: Update each file until the findings clear**

Where a doc shows a script command, write `pwsh ./scripts/<x>.ps1` and add the note "(Windows PowerShell 5.1: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/<x>.ps1`)" once per file.

- [ ] **Step 3: Add the CI job to `.github/workflows/build.yml`**

```yaml
  harness:
    name: Agent harness
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4
      - name: Pester
        shell: pwsh
        run: Invoke-Pester -Path tests/harness -CI
      - name: Drift, budgets, links, staleness, facts, specs, evals
        shell: pwsh
        run: ./scripts/sync-agent-harness.ps1 -Check
```

Linux paths are case-sensitive. If `Links` fails only in CI, fix the casing in the doc.

- [ ] **Step 4: Run the full verification**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync-agent-harness.ps1 -Check`
Expected: exit 0 in ≤ 60 s.

Run: `powershell -NoProfile -Command "Invoke-Pester -Path tests/harness -Output Detailed"`
Expected: all tests pass.

Run: `git status --short docs .agent/tickets | Select-String -NotMatch 'HARNESS-NATIVE-001|_templates/feature.md'`
Expected: empty.

- [ ] **Step 5: Hand-check (the human does this; record the result in the ticket)**

- In a new Claude Code session, `/context` lists 5 project skills. After reading `src/Services/Products.API/Program.cs`, `/memory` lists `.claude/rules/backend.md`.
- In VS Code Copilot agent mode, `/` lists the 10 project skills, and a `.tsx` edit shows `frontend.instructions.md` in References (AC-12).

- [ ] **Step 6: Close out**

Tick each AC in `specs/ai-native-harness/acceptance-criteria.md` with cited evidence. Set the spec `**Status:** shipped`. In the ticket, set `status: completed`, check all steps, and record before/after numbers.

```bash
git add -A .agent .claude/settings.json .github/workflows/build.yml README.md scripts/bootstrap.ps1 specs/ai-native-harness
git commit -m "docs(harness): document the canonical layout, specs and skill evals; gate in CI"
```
