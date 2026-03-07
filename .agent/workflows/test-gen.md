---
description: Generates a unit test for the currently open or specified file.
---

# Workflow: /test-gen
**Description:** Generates a unit test for the currently open or specified file.

## Steps
1. **Selection:** Ask user: "Which Service or Controller should I test?"
2. **Analysis:** Run `generate-dotnet-test` skill.
3. **Drafting:** Show the user the generated test code.
4. **File Creation:** Ask: "Should I save this to the Tests project now?"