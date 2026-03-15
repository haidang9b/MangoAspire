---
name: code-review
description: Analyze code for quality, performance, and security issues. Use when reviewing pull requests, auditing code, or seeking improvements on existing implementations.
---

# Code Review

This skill helps you analyze code for quality, performance, and security issues, providing actionable feedback to developers.

## When to Use

- Reviewing new code before merging
- Auditing existing code for vulnerabilities
- Checking for performance bottlenecks and idiomatic usage of C#/.NET and Frontend (React/JS/HTML)
- Analyzing code structure and maintainability

## When Not to Use

- Writing new feature code from scratch
- Running tests or builds directly

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

## Validation

- [ ] Identified at least one area of interest (e.g., security, performance, or quality improvement) or verified the code meets high standards.
- [ ] Suggestions are actionable, clear, and relevant to the provided code.
- [ ] No secrets or sensitive information are exposed in the feedback.

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Focusing only on syntax | Look beyond syntax to identify architectural and structural issues. |
| Being vague | Give specific, actionable recommendations with code examples. |
| Ignoring context | Consider the broader system context and requirements when reviewing. |
