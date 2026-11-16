---
name: code-review
description: Use when reviewing new code before merging, auditing existing code for vulnerabilities, or checking performance and idiomatic-usage concerns in this repo's C#/.NET or React code.
---

# Code Review

Analyze code for quality, performance, and security issues: new code before merging, an audit of
existing code for vulnerabilities, or a structure/maintainability pass. Not for writing new feature
code. Review only: never edit files during the review itself.

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| Code, file path, or "the current changes" | Yes | What to review. For current changes, read `git diff` (staged and unstaged). |
| Spec | No | `specs/<slug>/acceptance-criteria.md`, if the change implements a spec: check each AC is met |
| Focus | No | E.g. security or performance |

## Workflow

1. **Load the rules.** Read `references/rules.md`: the security, architecture, performance and
   idiomatic rules for the backend (C#/.NET) and frontend (React, JS, CSHTML, HTML).
2. **Understand the structure.** What the code does and what it depends on. Check it against the
   repo guardrails: Vertical Slice Architecture, `DbContext` directly in handlers (no repository
   layer), no cross-slice calls, FluentValidation validator for every command, `ResultModel<T>`,
   `DataVerificationException` for business errors.
3. **Security.** Hardcoded secrets, injection (SQL, XSS), authentication/authorization on new
   endpoints, secure data storage.
4. **Performance and quality.** Bottlenecks, needless allocations, missing `AsNoTracking()` on
   reads, deviations from C# 15 / React 19 idioms, dead code, testability.
5. **Report.** Group findings by **Backend** (Vertical Slice / C#) and **Frontend** (Context /
   hooks), each ranked **Critical**, **Warning** or **Suggestion**, with file:line and a concrete
   fix (an idiomatic code example where useful). If a spec was given, list each AC as met or not met.
6. **Fix loop (only when the user asks).** Offer to apply the fixes, such as modernization like
   primary constructors. After applying any, verify with `dotnet build MangoAspire.sln` and
   `pnpm --dir src/UI/mango-ui lint`.

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Focusing only on syntax | Look for architectural and structural issues too. |
| Being vague | Specific, actionable fixes with code examples. |
| Flagging a correct change | Say plainly when there are no Critical findings. |
| Editing during review | Report first; change code only when the user asks. |
