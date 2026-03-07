---
trigger: glob
globs: docs/**/*.md
---

# Rule: Documentation Standards

## Formatting Requirements
- **Language:** Use professional, technical English.
- **Code Blocks:** Always specify the language (e.g., ```csharp or ```tsx).
- **Headings:** Use a clear hierarchy starting from H1 (#) for titles.

## Content Focus
- **Backend:** Focus on Input/Output DTOs, HTTP Status Codes, and Dependency Injection requirements.
- **Frontend:** Focus on Component Props (TypeScript interfaces), Hook dependencies, and Global State interactions (RTK).
- **Onboarding:** Ensure that every new document includes a "How to run/test" section if applicable.

## Prohibited Content
- Do not include sensitive information like passwords, connection strings, or internal IP addresses.
- Do not use redundant prose; keep explanations concise and developer-oriented.