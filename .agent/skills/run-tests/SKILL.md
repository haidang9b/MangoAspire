---
name: run-project-tests
description: Use this skill to run all unit tests for both the .NET backend and React frontend.
---

# Instructions
1. Navigate to the `src/backend` directory and execute `dotnet test`.
2. Navigate to the `src/frontend` directory and execute `npm run test:unit`.
3. Capture the output. If there are failures, analyze the error logs and propose a fix.
4. Report a summary of "Passed/Failed" to the user.