---
name: generate-conventional-commit
description: Analyzes git diffs to generate professional Conventional Commits.
---

# Smart Commit Generator

Create standardized, semantic git commits using the Conventional Commits specification. Analyze the actual diff to determine appropriate type, scope, and message.

## When to Use

- When aiming to generate clear, structured, and professional git commit messages.
- When you want to ensure commits conform to the Conventional Commits specification.
- Before running `git commit`, to assist in grouping or messaging staged/unstaged changes.

## When Not to Use

- When working with a repository that enforces a non-standard commit format.
- Directly pushing or modifying history (e.g., rewriting pushed commits).

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| Context | No | Additional context about the focus of the changes over what git diff provides. |

## Conventional Commit Format

```text
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

## Commit Types

| Type       | Purpose                        |
| ---------- | ------------------------------ |
| `feat`     | New feature                    |
| `fix`      | Bug fix                        |
| `docs`     | Documentation only             |
| `style`    | Formatting/style (no logic)    |
| `refactor` | Code refactor (no feature/fix) |
| `perf`     | Performance improvement        |
| `test`     | Add/update tests               |
| `build`    | Build system/dependencies      |
| `ci`       | CI/config changes              |
| `chore`    | Maintenance/misc               |
| `revert`   | Revert commit                  |

## Breaking Changes

```text
# Exclamation mark after type/scope
feat!: remove deprecated endpoint

# BREAKING CHANGE footer
feat: allow config to extend other configs

BREAKING CHANGE: `extends` key behavior changed
```

## Workflow

### Step 1: Analyze Diff

Use the terminal to check the status and diffs.

```bash
# If files are staged, use staged diff
git diff --staged

# If nothing staged, use working tree diff
git diff

# Also check status
git status --porcelain
```

### Step 2: Stage Files (if needed)

If nothing is staged or you want to group changes differently:

```bash
# Stage specific files
git add path/to/file1 path/to/file2

# Stage by pattern
git add *.test.*
git add src/components/*

# Interactive staging
git add -p
```
> **Warning**: Never commit secrets (.env, credentials.json, private keys).

### Step 3: Generate Commit Message

Analyze the diff to determine:
- **Type**: What kind of change is this?
- **Scope**: What area/module is affected?
- **Description**: One-line summary of what changed (present tense, imperative mood, <72 chars)

### Step 4: Execute Commit

```bash
# Single line
git commit -m "<type>[scope]: <description>"

# Multi-line with body/footer
git commit -m "$(cat <<'EOF'
<type>[scope]: <description>

<optional body>

<optional footer>
EOF
)"
```

## Validation

- [ ] A commit is successfully generated using Conventional Commits format.
- [ ] No secrets are staged or included in the commit.
- [ ] Description is in imperative mood and under 72 characters.

## Best Practices & Git Safety Protocol

- One logical change per commit
- Present tense: "add" not "added"
- Imperative mood: "fix bug" not "fixes bug"
- Reference issues: `Closes #123`, `Refs #456`
- Keep description under 72 characters
- **NEVER** update git config
- **NEVER** run destructive commands (`--force`, `hard reset`) without explicit request
- **NEVER** skip hooks (`--no-verify`) unless user asks
- **NEVER** force push to main/master
- If commit fails due to hooks, fix and create NEW commit (don't amend)

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Committing secrets | Check `git diff` carefully before staging or committing credentials. |
| Vague commit messages | Avoid phrases like "Update files" or "Fix stuff". Use specific scope and descriptions. |
| Inconsistent phrasing | Remember to use imperative mood ("add", not "added" or "adds"). |
