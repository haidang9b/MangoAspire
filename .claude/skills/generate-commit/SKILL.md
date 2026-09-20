---
name: generate-commit
description: Use when about to run git commit and the message should be Conventional Commits format derived from the actual diff, not guessed.
---

<!-- GENERATED from .agent/ by scripts/sync-agent-harness.ps1 - do not edit directly -->
# Smart Commit Generator

Generate a Conventional Commits message from the actual diff — type, scope and description derived
from what changed, not guessed.

## Commit Types

| Type | Purpose | Type | Purpose |
|---|---|---|---|
| `feat` | New feature | `test` | Add/update tests |
| `fix` | Bug fix | `build` | Build system/dependencies |
| `docs` | Documentation only | `ci` | CI/config changes |
| `style` | Formatting, no logic | `chore` | Maintenance/misc |
| `refactor` | No feature/fix | `revert` | Revert commit |
| `perf` | Performance | | |

Breaking change: `feat!: <description>` or a `BREAKING CHANGE: <detail>` footer.

## Workflow

1. **Analyze:** `git diff --staged` (or `git diff` if nothing staged) plus `git status --porcelain`.
2. **Stage** if needed (`git add <path>`, or `-p` for interactive). Never stage secrets
   (`.env`, credentials, keys) — check the diff first.
3. **Compose:** `<type>[scope]: <description>` — imperative mood, present tense, under 72 chars.
   Multi-line body/footer via a heredoc when needed.
4. **Commit.**

## Git Safety Protocol
- Never update git config; never run `--force`/hard reset without explicit request; never skip
  hooks (`--no-verify`) unless asked; never force-push to main/master.
- If a hook fails, fix the issue and create a **new** commit — don't amend.
- One logical change per commit; reference issues (`Closes #123`) where relevant.

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Committing secrets | Check the diff carefully before staging. |
| Vague messages ("update files") | Be specific about type, scope, and what changed. |
| Inconsistent mood | Imperative: "add", not "added"/"adds". |
