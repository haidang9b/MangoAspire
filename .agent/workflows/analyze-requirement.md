---
description: Orchestrates the transformation of PO requirements into a developer-ready plan.
---

# Workflow: analyze-requirement
Description: Orchestrates the transformation of PO requirements into a developer-ready plan.

## Execution Steps
1. **Intake:** Prompt the user to paste the requirement from the PO.
2. **Analysis:** Run the 'analyze-ticket' skill.
3. **Review:** Present the "Technical Blueprint" and the "Clarifying Questions."
4. **Approval:** Once the questions are answered, update the blueprint.
5. **Next Step:** Suggest running `implement-ticket` using this blueprint as the source of truth.