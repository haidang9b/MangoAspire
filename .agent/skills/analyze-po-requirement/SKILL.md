---
name: analyze-po-requirement
description: Translates high-level Product Owner (PO) requirements into a technical blueprint for the MangoAspire Vertical Slice backend and React frontend.
---

# Analyze PO Requirement

Turn a raw PO requirement into a structured technical blueprint for MangoAspire (.NET 10 + Aspire
microservices, React/Vite SPA, **Vertical Slice Architecture** — see `AGENTS.md` and
`.agent/rules/architecture.md`). Use for a new story/epic/feature; not for requirements already
fully specified with entities/DTOs/components, and not for writing the implementation itself.

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| PO Requirement | Yes | The raw feature/business need description. |

## Workflow

1. **Intake:** read the requirement; ask immediate clarifying questions if the business goal is
   unclear.
2. **Impact analysis per affected service:**
   - **Feature slice:** command/query, handler, DTOs, FluentValidation validator. `DbContext`
     directly in handlers — no separate repository layer.
   - **Endpoint:** Minimal API route in `Routes/`, `ResultModel<T>` shape.
   - **Persistence:** new tables / EF Core migrations / `DbContext` config; cross-service events
     (RabbitMQ) if the change spans services.
   - **Frontend:** React components, Context updates, `src/UI/mango-ui/src/api/` client calls via
     hooks.
3. **Edge cases:** i18n/currency/timezone handling, nullable/incomplete input, auth implications
   (provider-toggled: Duende / OpenIddict).
4. **Output** — a blueprint the `implement-ticket` workflow can consume:
   - **Business Summary** — what and why, briefly.
   - **Technical Impact Map** — backend (slice/handler/validator/DTOs/routes) and frontend
     (components/Context/API calls).
   - **Acceptance Criteria** — Given/When/Then. Backend tests: xUnit + Moq + Shouldly. Frontend has
     **no test runner** — verification is `pnpm --dir src/UI/mango-ui lint` + `build`.
   - **Clarifying Questions** — 3-5, to resolve before implementation starts.

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Skipping frontend or backend impact | Map both sides of the stack. |
| Vague acceptance criteria | Strict Given/When/Then for every AC. |
| Inventing a frontend test runner | None exists — use `pnpm lint` + `build`. |
| Adding extra layers | Vertical Slice only; no Repository/Application/Domain layers. |
