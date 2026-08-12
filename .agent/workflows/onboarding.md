---
description: Performs a deep scan of the repo to explain it to a new dev.
---

# Workflow: /onboard
**Description:** Performs a deep scan of the repo to explain it to a new dev.

## Steps
1. **Trigger Skill:** Run `onboard-analyzer`.
2. **Context Search:** Search `README.md` and `/docs`, then read `.agent/memory/index.md` and follow
   the links that match the developer's area. Point them at `.agent/ui/board.html` for what is
   currently in flight, and at `.agent/README.md` for how the harness itself is laid out.
3. **Report:** Provide a "Day 1 Guide" for the developer.
4. **Interactive:** Ask: "Which part of the stack (Frontend or Backend) would you like to explore first?"