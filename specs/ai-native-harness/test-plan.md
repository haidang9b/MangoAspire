# Test plan

## Coverage strategy
- **Pester** (unit): `tests/harness/AgentHarness.Tests.ps1` and `tests/harness/SkillEval.Tests.ps1`, on fixture repos in `$TestDrive`, with a fake `claude` invoker.
- **Lint**: `scripts/sync-agent-harness.ps1 -Check` against the real repo.
- **CI**: the `harness` job in `.github/workflows/build.yml` runs Pester and `-Check` on Linux.
- **Eval run**: `scripts/eval-skills.ps1` against real `claude` sessions; on demand only.
- **Manual**: a hand-check in Claude Code and VS Code Copilot.

| AC    | Pester | Lint `-Check` | CI | Eval run | Manual |
|-------|--------|---------------|----|----------|--------|
| AC-1  |   x    |  x (Budget)   | x  |          |        |
| AC-2  |   x    |  x (Skills)   | x  |          |   x    |
| AC-3  |   x    |  x (Rules, Drift) | x |       |   x    |
| AC-4  |   x    |  x (Drift)    | x  |          |        |
| AC-5  |   x    |  x (Links, Staleness) | x |   |        |
| AC-6  |   x    |  x (Facts)    | x  |          |        |
| AC-7  |   x    |  x (Specs)    | x  |          |        |
| AC-8  |        |               |    |    x     |   x    |
| AC-9  |   x    |  x (Evals)    | x  |    x     |        |
| AC-10 |        |  x (Evals)    |    |    x     |        |
| AC-11 |        |  x (Staleness, Links) | x |   |   x    |
| AC-12 |        |               |    |          |   x    |

## Scenarios
- Priority 1: lint and sync on a valid fixture produce no findings; drift appears after an unsynced edit; the eval runner leaves the main tree clean.
- Priority 2: each lint category flags its own defect (oversize, prose `paths`, name mismatch, a missing path or link, a stale term, a missing symbol, a bad spec, a bad eval set).
- Priority 3: CRLF mirror files; an invoker that throws; grader output that isn't JSON; placeholders, globs, fenced code, file names and acronyms that must not be flagged.
- Priority 4: the context report numbers; `-DryRun` output.

## Risks
- Facts heuristics may flag illustrative names. Mitigation: an allowlist with a mandatory reason.
- `claude -p` flags and tool names (Bash vs PowerShell tool on Windows) are confirmed only by the Task 10 smoke run.
- LLM grading is noisy. Mitigation: deterministic checks wherever possible, and `-Runs` for repeats.
- Manual AC-12 depends on VS Code settings defaults.
