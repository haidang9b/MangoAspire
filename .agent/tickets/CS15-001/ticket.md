---
id: CS15-001
kind: refactor
status: completed
priority: low
createdUtc: 2026-11-13T00:00:00Z
updatedUtc: 2026-11-13T00:00:00Z
completedUtc: 2026-11-13T00:00:00Z
tags: [csharp15, dotnet11, chatagent]
---

# CS15-001 — Adopt low-risk C# 15 syntax in the ChatAgent guards

## Plan

### Motivation
The solution already compiles as C# 15 (`net11.0` default, SDK `11.0.100`, no `LangVersion` pin).
An audit of the whole `src/` tree against the C# 15 feature list found two places where the new
syntax removes bookkeeping without changing behavior.

### Before / after
- `ChatAgent.App/Guards/RelevanceGuard.cs` — `GetCategoryTermsAsync` built a case-insensitive
  `HashSet` with a nested `foreach`/`Add`; now one collection expression:
  `[with(StringComparer.OrdinalIgnoreCase), .. categories.Concat(productNames).SelectMany(Tokenize)]`.
  `Tokenize` takes the same `with(...)` form for its empty set.
- `ChatAgent.App/Guards/Output/RevisionValidator.cs` — `IsDeletionOnly` used a `found` flag to
  carry the inner loop's result out; now `continue nextWord;` targets the outer loop directly.
  The label is flush-left per `csharp_indent_labels = flush_left` in `.editorconfig`.

### Risk and rollback
Pure syntax change; same set contents and same subsequence result. Rollback is reverting the two
files.

### Steps
Edit the two files, then build, format-check and test `ChatAgent.App`.

### Verification
`dotnet build` of `ChatAgent.App` (0 warnings), `dotnet format --verify-no-changes` on the two
files, `dotnet test` of `ChatAgent.App.Tests` — `RelevanceGuardTests` (catalogue terms via
in-memory DB) and `RevisionValidatorTests` (accept/reject/reorder cases) cover both paths.

## Steps
- [x] **Step 1 — Scope and plan**
  - [x] Define motivation and the before/after shape
  - [x] Identify risk and rollback strategy
  - [x] Write the plan above
  - [x] Plan approved by the user
- [x] **Step 2 — Execute**
  - [x] Carry out the refactor
- [x] **Step 3 — Verification**
  - [x] Build/tests green with behavior unchanged
  - [x] Zero compiler warnings (fix-warnings)
- [x] **Step 4 — Documentation, memory and completion**
  - [x] ~~Sync `/docs`, if the refactor changes documented structure~~ — skipped: no documented structure changed
  - [x] Conventional commit created
  - [x] Promote Decisions/Gotchas below into `.agent/memory/domains/`

## Links
- Branch: `refactor/CS15-001-csharp15-syntax`
- Commits: `refactor(chat-agent): adopt C# 15 syntax in guards (CS15-001)`
- PRs:
- Files: `src/Services/ChatAgent.App/Guards/RelevanceGuard.cs`, `src/Services/ChatAgent.App/Guards/Output/RevisionValidator.cs`
- Docs:
- Memory: `.agent/memory/stack.md` (C# 15 line)

## Decisions
- **Unions / closed hierarchies not adopted.** `ResultModel<T>` is the JSON wire contract and
  AGENTS.md mandates it plus exceptions; `IntegrationEvent` subtypes live outside the `EventBus`
  assembly, which `closed` forbids. `GuardVerdict`/`ReviewVerdict` could become a union / closed
  record, but tests read `.Allowed` (18×) and `.Kind` (13×) and the record's constructor arity is
  deliberately frozen — deferred until the guards are reworked anyway.
- **Extension indexers, memory-safety preview, older-runtime static interface members** — no
  candidates in this codebase.

## Gotchas
- `.editorconfig` sets `csharp_indent_labels = flush_left`: a labeled loop's label goes at column 0,
  or `dotnet format` will move it.

## Open Questions
## Blockers

## Session Log
### 2026-11-13
Audited `src/` for C# 15 applicability; confirmed no C# 15 breaking-change triggers and a clean
`--no-incremental` solution build. Applied the two changes above; `ChatAgent.App` builds with 0
warnings, format check clean, `ChatAgent.App.Tests` 217/217 passed. User approved; committed on
`refactor/CS15-001-csharp15-syntax`.
