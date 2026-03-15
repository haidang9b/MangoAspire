---
name: analyze-po-requirement
description: Translates high-level Product Owner (PO) requirements into technical specifications for Clean Architecture, covering both React frontend and .NET backend.
---

# Analyze PO Requirement

Transform a raw Product Owner (PO) requirement into a structured technical plan, ensuring all edge cases for the Overseas Order Management System are covered. This skill bridges the gap between business needs and technical implementation across a React frontend and .NET 10 backend.

## When to Use

- When receiving a new agile story, epic, or feature request from a Product Owner.
- When planning the technical architecture for a new feature in the Overseas Order Management System.
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
1. Read the provided PO description carefully (e.g., "We need a way to track shipping status for overseas orders").
2. Ask any immediate clarifying questions if the core business goal is entirely unclear.

### Step 2: Impact Analysis
1. **Domain:** Identify new Entities (e.g., `ShippingLog`) or Value Objects needed.
2. **Application:** Identify new DTOs, Application Service methods, and Clean Architecture Use Cases.
3. **Infrastructure:** Determine if new Database tables, EF Core migrations, or external API integrations (e.g., DHL/FedEx) are required.
4. **Frontend:** Identify new React UI components, React Context updates, or React Query hooks required.

### Step 3: Edge Case Detection
1. Identify potential localization issues, such as Timezone differences or Currency conversions.
2. Consider edge cases related to incomplete data, Nullable fields, or unexpected user inputs.
3. Document security or permission implications for the new feature.

### Step 4: Output Generation
Produce a structured "Technical Blueprint" that the `implement-ticket` workflow can use. The output must follow the structure defined in Output Structure.

## Output Structure

Generate a markdown document containing the following sections:

### 📝 Business Summary
Brief overview of the "What" and the "Why" behind the requirement.

### 🏗️ Technical Impact Map
- **Backend:** Entities, DTOs, and Minimal API routes or Controllers.
- **Frontend:** React Components, Context usage, and API hooks (e.g., React Query).

### 🧪 Acceptance Criteria (AC)
- Formatted as: "Given [context], when [action], then [result]."
- Include specific testing requirements for both backend (xUnit) and frontend (Vitest).

### ❓ Clarifying Questions
List 3-5 crucial questions to ask the PO or Developer to clear up ambiguity before implementation begins.

## Validation

- [ ] The output document contains all required sections (Business Summary, Technical Impact Map, Acceptance Criteria, Clarifying Questions).
- [ ] Technical impact covers both the React frontend and .NET Clean Architecture backend.
- [ ] Acceptance criteria include explicit mentions of xUnit and Vitest testing requirements.
- [ ] At least 3 clarifying questions are generated to address potential ambiguities or edge cases.

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Skipping frontend or backend impact | Ensure the technical analysis maps entirely across the stack (React + .NET 10). |
| Vague acceptance criteria | Use strict BDD format ("Given... When... Then...") for all ACs. |
| Forgetting testing requirements | Explicitly mention xUnit (backend) and Vitest (frontend) in the testing plan. |
| Over-engineering the solution | Stick to Lean Clean Architecture principles; only propose new layers or entities if strictly necessary. |
