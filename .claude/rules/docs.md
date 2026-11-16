---
paths:
  - "docs/**/*.md"
---

# Documentation standards (`/docs`, human documentation)

- Professional, concise technical English; no redundant prose.
- An H1 title, then a clear heading hierarchy. Every code block names its language (`csharp`, `tsx`, `bash`, `powershell`).
- **Backend docs:** input/output DTOs, HTTP status codes and DI requirements.
- **Frontend docs:** component props (TypeScript interfaces), hook dependencies, and the React Context they read or write.
- New guides include a "How to run / test" section where it applies.
- Never include secrets, connection strings or internal IP addresses.
- `/docs` holds no agent memory (`.agent/memory/`), ticket state (`.agent/tickets/`) or feature specs (`specs/`).
