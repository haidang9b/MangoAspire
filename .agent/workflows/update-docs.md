---
description: Triggers a comprehensive update of the project documentation.
---

# Workflow: update-docs
Description: Triggers a comprehensive update of the project documentation.

## Execution Steps
1. **Analyze:** Run the 'manage-docs' skill to identify out-of-sync documentation.
2. **Preview:** Present a summary of proposed changes to the user (e.g., "Updating OrderService docs").
3. **Apply:** Once the user approves, write the changes to the filesystem.
4. **Commit:** Suggest running 'generate-commit' to include the documentation updates in the current branch.