# Acceptance criteria

Each item is a checkable, observable statement. If it isn't testable, rewrite it.

- [ ] AC-1: Given a new Claude Code session in the repo, when it starts, then the always-on project context (`CLAUDE.md`, its `@` imports, and rules without `paths`) is ≤ 5,120 bytes, as reported by `scripts/sync-agent-harness.ps1`.
- [ ] AC-2: Given the project skills, when a session starts, then at most 6 skills expose a description to the model, and the 5 human-run workflows (`implement-ticket`, `analyze-requirement`, `onboarding`, `update-docs`, `fix-warnings`) are slash-invocable with `disable-model-invocation: true`.
- [ ] AC-3: Given an agent edits a file matching a rule's `paths`, when the agent reads that file, then Claude Code loads the rule from `.claude/rules/`, and Copilot receives the same text from `.github/instructions/` with an `applyTo` equal to the rule's globs.
- [ ] AC-4: Given a change to `.claude/rules/` or `.claude/agents/` that is not followed by a sync, when CI runs, then the harness job fails with a drift finding. CRLF-only differences never count as drift.
- [ ] AC-5: Given the harness files (`AGENTS.md`, `CLAUDE.md`, `.claude/**`, `.agent/README.md`, `.agent/tools/`, `.agent/memory/`) and `specs/`, when `-Check` runs, then every backticked repo path in the harness files resolves, every relative markdown link in the harness files and `specs/` resolves, and no term from the staleness list appears in the harness files.
- [ ] AC-6: Given harness content that names a code symbol or package in backticks, when `-Check` runs, then it fails unless the name appears in the source corpus (`src/`, `tests/`, solution and package manifests) or is on `scripts/harness/facts-allowlist.txt` with a reason.
- [ ] AC-7: Given a folder under `specs/` other than `_template`, when `-Check` runs, then it fails unless `spec.md` has a valid `**Status:**`, `acceptance-criteria.md` has at least one uniquely numbered `AC-n`, and `test-plan.md` maps exactly the defined AC ids.
- [ ] AC-8: Given a `feature` ticket, when `/implement-ticket` plans it, then it creates `specs/<slug>/` from `specs/_template/` and links it from `ticket.md`. When it verifies, it marks each AC-n met (`[x]`, with cited evidence) or lists it as unmet.
- [ ] AC-9: Given `tests/skill-evals/<skill>/evals.json`, when `scripts/eval-skills.ps1` runs, then each eval runs once with the skill and once without it, each in a disposable git worktree, and is graded on deterministic checks plus LLM-graded expectations. A summary reports per-skill pass rates, the delta, a verdict, cost and duration, and the developer's working tree is left unchanged.
- [ ] AC-10: Given `code-review`, `create-unit-test`, `run-tests`, `generate-commit`, `add-feature-slice` and `analyze-requirement`, when the first full eval run completes, then each skill has ≥ 3 evals and its results are recorded in the ticket. Every skill whose verdict isn't `helps` has a follow-up item.
- [ ] AC-11: Given the repository after this change, when it is inspected, then every rule, skill and agent exists exactly once under `.claude/`. `.agent/{skills,agents,workflows,rules}`, `.claude/commands`, `.agent/CODING_CONVENTIONS.md` and `.agent/API_PROJECT_STRUCTURE.md` no longer exist.
- [ ] AC-12: Given VS Code Copilot Chat in agent mode, when the user types `/`, then the 10 project skills are listed. When it edits a `.tsx` file under `src/UI/mango-ui/src/`, then `frontend.instructions.md` is applied.

## Non-functional
- [ ] Portability: every script and module runs unchanged on Windows PowerShell 5.1 and on PowerShell 7 on Linux.
- [ ] Performance: `sync-agent-harness.ps1 -Check`, including Facts, finishes in ≤ 60 s on the dev machine.
- [ ] Safety: sync writes only `.github/instructions/` and `.github/agents/`. The eval runner never modifies the main working tree and removes its worktrees even on failure.
- [ ] Cost: no `claude` invocation runs in CI. `eval-skills.ps1 -DryRun` lists every planned invocation without running any.

## Out of scope (reminder)
- Trigger-accuracy evals, AC-vs-code review reports, and the agentic-devkit `planning/` flow and commands. See `spec.md` → Out.
