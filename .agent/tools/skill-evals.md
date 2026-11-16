# Skill output evals

`scripts/eval-skills.ps1` measures whether a skill makes the agent's output better. It runs each eval
twice through `claude -p`, once **with** the skill (prompt `/<skill> <prompt>`) and once
**without** it (the skill folder is hidden and the prompt is plain), then grades both. The method
comes from the agentic-devkit `skill-creator`, adapted to a scripted runner.

Runs cost tokens and need `claude` auth, so they are on demand only and **never in CI**. Always
start with `-DryRun`.

## Run

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/eval-skills.ps1 -Skill generate-commit -DryRun
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/eval-skills.ps1 -Skill generate-commit -EvalId 1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/eval-skills.ps1 -Skill code-review,run-tests -Runs 2 -Model claude-sonnet-5-5
```

Each run happens in a disposable `git worktree` of the committed `HEAD`, so **commit skill and eval
changes first**. Uncommitted edits are not evaluated. The developer's working tree is never touched.
Before the session starts, `tests/skill-evals/` is hidden (skip-worktree) so it can't read the
expected answers.

## Write an eval set

`tests/skill-evals/<skill>/evals.json` follows the devkit schema plus `name`, `setup` and `checks`:

```json
{
  "skill_name": "generate-commit",
  "evals": [
    {
      "id": 1,
      "name": "fix-from-null-guard",
      "prompt": "Write a commit message for the staged changes. Do not run git commit.",
      "expected_output": "A fix(products...) Conventional Commit ...",
      "setup": ["git apply tests/skill-evals/generate-commit/files/keep-catalog-type.patch", "git add -A"],
      "checks": [{ "type": "no-commit" }, { "type": "response-matches", "pattern": "(?m)^\\s*fix(\\([^)]*\\))?!?: " }],
      "expectations": ["The message is a Conventional Commit with type fix"]
    }
  ]
}
```

- **`setup`:** PowerShell commands run in the worktree before the session. Fixture patches live in
  `files/` and are kept byte-exact by `.gitattributes`.
- **`checks`** are deterministic:
  - `command` passes on exit 0 (or `expect_exit`);
  - `file-matches` passes if any file under `glob` matches `pattern`;
  - `no-edits` passes if the working tree is unchanged since setup;
  - `no-commit` passes if HEAD is unchanged;
  - `response-matches` applies a regex to the final response.
- **`expectations`** are graded by a separate `claude -p` call using `tests/skill-evals/_grader.md`.
  Write them as verifiable sentences. Prefer a check whenever the outcome can be checked mechanically.
- `-Check -Category Evals` validates every eval set.

## Read the results

`tests/skill-evals/_results/<timestamp>/` (gitignored) holds:
- per run: `outputs/` (response, diff), `checks.json`, `grading.json` (`text`/`passed`/`evidence`,
  in the skill-creator schema) and `meta.json`;
- per batch: `benchmark.json` and `summary.md`.

| Verdict | Meaning |
|---|---|
| `helps` | with-skill pass rate − without-skill ≥ +0.10 |
| `neutral` | within ±0.10: the model does as well without the skill. Make the skill cheaper or retire it, or write harder evals |
| `hurts` | ≤ −0.10: the skill steers the model wrong; fix it |

**Non-discriminating** expectations pass in both configs. They show what the evals fail to measure,
not what the skill adds.
