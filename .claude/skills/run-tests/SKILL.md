---
name: run-tests
description: Use this skill to run the .NET unit test suite (xUnit) for MangoAspire and triage failures.
---

<!-- GENERATED from .agent/ by scripts/sync-agent-harness.ps1 - do not edit directly -->
# Instructions

1. From the repository root, run the full suite: `dotnet test MangoAspire.sln`.
   - For a single project: `dotnet test tests/Services/Products.API.Tests/Products.API.Tests.csproj`.
   - For a single class/method, add `--filter "FullyQualifiedName~<Class>.<Method>"`.
2. Capture the output. If there are failures, read the error logs, locate the failing test and the
   code under test, and propose a fix (follow AGENTS.md conventions).
3. Report a concise "Passed / Failed / Skipped" summary to the user, plus any proposed fixes.

## Notes

- Test stack: **xUnit + Moq + Shouldly** (see `.agent/rules/backend-testing.md`).
- There is **no frontend test runner** configured for `src/UI/mango-ui` (see AGENTS.md). Do not invent
  `npm`/`pnpm` test commands. For frontend changes, run `pnpm --dir src/UI/mango-ui lint` and
  `pnpm --dir src/UI/mango-ui build` instead.
