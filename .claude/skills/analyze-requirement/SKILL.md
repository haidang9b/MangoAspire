---
name: analyze-requirement
description: Use when the user runs /analyze-requirement to turn a PO requirement into a spec under specs/<slug>/ with testable acceptance criteria and a test plan.
disable-model-invocation: true
---

# Analyze Requirement

Turn a raw PO requirement into a spec in the agentic-devkit format: `specs/<slug>/spec.md`,
`acceptance-criteria.md` and `test-plan.md`, plus the technical blueprint for MangoAspire (.NET 11 +
Aspire microservices, React/Vite SPA, Vertical Slice Architecture). Not for writing the
implementation.

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| PO requirement | Yes | The raw feature or business need. If missing, ask the user to paste it. |
| Slug | No | Kebab-case folder name. Derive one from the requirement if not given. |

## Workflow

1. **Intake.** Read the requirement. List what is decided and what isn't.
2. **Scaffold.** Copy `specs/_template/` to `specs/<slug>/`. Set `**Status:** in-review`, the owner
   and today's date.
3. **spec.md.** Problem, User & outcome, Scope In/Out, Constraints & assumptions. Anything
   undecided goes under **Open questions**, with an owner. Never resolve it by inventing an answer.
4. **acceptance-criteria.md.** One `- [ ] AC-n: Given … when … then …` per observable behaviour,
   numbered from AC-1, each testable. Add Non-functional items where they apply (auth on new
   endpoints, performance). Never write an AC for an open question.
5. **Technical impact.** Record it in `spec.md` → Constraints & assumptions, or in a `design.md`
   beside the spec, per affected service:
   - **Feature slice:** command/query, handler, DTOs, FluentValidation validator, and `DbContext`
     used directly in handlers (no repository layer).
   - **Endpoint:** a Minimal API route in `Routes/` (kebab-case REST noun) returning `ResultModel<T>`.
   - **Persistence:** tables, EF Core migration, `DbContext` config; integration events (RabbitMQ)
     if the change spans services.
   - **Frontend:** components, the existing Context to reuse, and client calls in
     `src/UI/mango-ui/src/api/` consumed through hooks.
   - **Edge cases:** i18n (EN/VI), currency, timezones, nullable input, and auth under both identity
     providers (Duende / OpenIddict).
6. **test-plan.md.** Map **every** AC in the coverage table (`Unit (xUnit)` / `Integration` /
   `Lint/Build` / `Manual`). The SPA has no test runner, so frontend ACs map to Lint/Build or Manual.
7. **Validate.** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync-agent-harness.ps1 -Check -Category Specs`
   must report no findings for `specs/<slug>/`.
8. **Report.** Summarize the spec, list the 3–5 most important open questions for the PO, and
   suggest `/implement-ticket` once the user approves (which sets Status `approved`).

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Inventing answers to undecided points | Put them under Open questions; no AC until decided. |
| Vague ACs | Strict Given/When/Then with an observable result. |
| An AC missing from the test plan | Every AC-n appears in the coverage table; the Specs lint checks this. |
| Inventing a frontend test runner | None exists; use Lint/Build or Manual. |
| Adding layers | Vertical Slice only; no Repository/Application/Domain layers. |
