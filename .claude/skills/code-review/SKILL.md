---
name: code-review
description: Use when reviewing new code before merging, auditing existing code for vulnerabilities, or checking performance and idiomatic-usage concerns in this repo's C#/.NET or React code.
---

# Code Review

Analyze code for quality, performance, and security issues — new code before merging, an existing
vulnerability audit, or a structure/maintainability pass. Not for writing new feature code or
running tests/builds directly.

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| Code snippet or file path | Yes | The code to review |
| Context/Requirements | No | Any specific focus areas (e.g., security, performance) |

## Workflow

### Step 1: Read the Review Rules Reference

Load the rigorous code review rules from `references/rules.md`. These rules dictate the security, architecture, performance, and idiomatic guidelines you must enforce for both Backend (C# .NET) and Frontend (React, JS, CSHTML, HTML).

### Step 2: Analyze Code Structure

Read and understand the overall architecture and design of the provided code. Identify its main purpose and dependencies.

### Step 3: Check for Security Vulnerabilities

Examine the code for common security flaws, such as hardcoded secrets, injection vulnerabilities (e.g., SQL injection, XSS), proper authentication and authorization handling, and secure data storage.

### Step 4: Evaluate Performance and Quality

Identify performance bottlenecks, inefficient algorithms, or unnecessary memory allocations. Check for deviations from C#/.NET and Frontend best practices, dead code, lack of testability, and maintainability concerns.

### Step 5: Suggest Actionable Improvements

Formulate specific, actionable recommendations for each identified issue. Prioritize findings into Critical, Warning, and Suggestion categories. Provide idiomatic code examples where appropriate.

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Focusing only on syntax | Look beyond syntax to identify architectural and structural issues. |
| Being vague | Give specific, actionable recommendations with code examples. |
| Ignoring context | Consider the broader system context and requirements when reviewing. |
