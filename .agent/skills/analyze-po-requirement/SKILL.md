---
name: analyze-po-requirement
description: Translates high-level Product Owner (PO) requirements into a technical blueprint for the MangoAspire Vertical Slice backend and React frontend.
---

# Analyze PO Requirement

Transform a raw Product Owner (PO) requirement into a structured technical plan for MangoAspire (a .NET 10 + Aspire microservices e-commerce platform with a React/Vite SPA). This skill bridges the gap between business needs and technical implementation across the React frontend and the .NET backend, following **Vertical Slice Architecture** (see AGENTS.md and `.agent/rules/architecture.md`).

## When to Use

- When receiving a new agile story, epic, or feature request from a Product Owner.
- When planning the technical approach for a new feature in one of the Mango services (Products, Coupons, Orders, ShoppingCart, Payments, ChatAgent, Identity).
- When breaking down high-level business requirements into actionable development tasks.

## When Not to Use

- When writing code or executing technical implementation (this is an analysis and planning skill).
- When the requirements are already fully specified with technical details (entities, DTOs, components).

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| PO Requirement | Yes | The raw description of the feature or business need. |

## Workflow

### Step 1: Requirement Intake
1. Read the provided PO description carefully (e.g., "We need to let customers apply multiple coupons to a cart").
2. Ask any immediate clarifying questions if the core business goal is unclear.

### Step 2: Impact Analysis (per affected service)
1. **Feature slice:** Identify the new command/query, handler, DTOs, and FluentValidation validator that make up the vertical slice. Use the `DbContext` directly in handlers (no Repository pattern).
2. **Endpoint:** Identify the Minimal API route(s) in `Routes/` and the `ResultModel<T>` shape returned.
3. **Persistence:** Determine whether new tables, EF Core migrations, or `DbContext` configuration changes are required. Consider cross-service events (RabbitMQ / event bus) if the change spans services.
4. **Frontend:** Identify new React components, Context updates, and `src/UI/mango-ui/src/api/` client calls consumed through hooks.

### Step 3: Edge Case Detection
1. Identify localization concerns (i18n, currency, timezone/datetime handling).
2. Consider edge cases around incomplete data, nullable fields, and unexpected input.
3. Document security or permission implications (auth is provider-toggled: Duende / OpenIddict).

### Step 4: Output Generation
Produce a structured "Technical Blueprint" that the `implement-ticket` workflow can consume. Follow the Output Structure below.

## Output Structure

Generate a markdown document containing the following sections:

### 📝 Business Summary
Brief overview of the "What" and the "Why" behind the requirement.

### 🏗️ Technical Impact Map
- **Backend:** Affected service, feature slice (command/query + handler + validator), DTOs, and Minimal API routes.
- **Frontend:** React components, Context usage, and API client calls/hooks.

### 🧪 Acceptance Criteria (AC)
- Formatted as: "Given [context], when [action], then [result]."
- Include backend testing requirements with **xUnit + Moq + Shouldly**. Note there is **no frontend test runner** configured; frontend verification is `pnpm --dir src/UI/mango-ui lint` + `build`.

### ❓ Clarifying Questions
List 3-5 crucial questions to ask the PO or developer to clear up ambiguity before implementation begins.

## Validation

- [ ] The output contains all required sections (Business Summary, Technical Impact Map, Acceptance Criteria, Clarifying Questions).
- [ ] Technical impact covers both the React frontend and the .NET Vertical Slice backend.
- [ ] Acceptance criteria include explicit backend xUnit testing requirements (and note the absence of a frontend runner).
- [ ] At least 3 clarifying questions are generated.

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Skipping frontend or backend impact | Ensure the analysis maps across the stack (React + .NET 10). |
| Vague acceptance criteria | Use strict BDD format ("Given... When... Then...") for all ACs. |
| Inventing a frontend test runner | None is configured — verify the SPA with `pnpm --dir src/UI/mango-ui lint` + `build`. |
| Proposing extra layers | Stick to Vertical Slice; do not add Repository/Application/Domain layers unless maintainers ask. |
