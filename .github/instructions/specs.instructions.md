---
applyTo: "specs/**/*.md"
---

<!-- GENERATED from .claude/ by scripts/sync-agent-harness.ps1 - do not edit directly -->

# Feature specs (agentic-devkit format)

- Each feature has `specs/<slug>/` with `spec.md`, `acceptance-criteria.md` and `test-plan.md`, copied from `specs/_template/`. A `design.md` or `plan.md` may sit beside them.
- `spec.md` carries exactly one status line: `**Status:** draft`, `in-review`, `approved` or `shipped`. The lifecycle is draft → in-review → approved (by the user) → shipped.
- ACs are `- [ ] AC-n: Given … when … then …`, observable and testable. Undecided points go under Open questions, never into an AC.
- Never renumber an approved AC; append new ones instead.
- `test-plan.md` maps **every** AC id in its coverage table, and no undefined ones.
- Tick an AC `[x]` only with cited evidence: a test name, or a command plus its output line.
- Check with `scripts/sync-agent-harness.ps1 -Check -Category Specs`.
