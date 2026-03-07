---
name: "BA: Analyze PO Requirement"
description: "Translates high-level PO requirements into technical specifications for Clean Architecture."
---

# Skill: BA Requirement Analyzer

## Goal
Transform a raw PO requirement into a structured technical plan, ensuring all edge cases for the Overseas Order Management System are covered.

## Logic Flow
1. **Requirement Intake:** Read the PO's description (e.g., "We need a way to track shipping status for overseas orders").
2. **Impact Analysis:**
   - **Domain:** Identify new Entities (e.g., `ShippingLog`) or Value Objects.
   - **Application:** Identify new DTOs and Service methods needed.
   - **Infrastructure:** Determine if new Database tables or external API integrations (e.g., DHL/FedEx) are required.
   - **Frontend:** Identify new UI components, Context updates, or React Query hooks.
3. **Edge Case Detection:** Identify potential issues like Timezone differences, Currency conversion, or Nullable data.
4. **Output Generation:** Produce a "Technical Blueprint" that the `implement-ticket` workflow can use.

## Output Structure
### 📝 Business Summary
Brief overview of the "What" and "Why."

### 🏗️ Technical Impact Map
- **Backend:** Entities, DTOs, and Minimal API routes.
- **Frontend:** Components, Context usage, and API hooks.

### 🧪 Acceptance Criteria (AC)
- "Given [context], when [action], then [result]."
- Include specific testing requirements for xUnit and Vitest.

### ❓ Clarifying Questions
List of 3-5 questions to ask the PO or Developer to clear up ambiguity.