---
name: create-unit-test
description: Use when a C# class or method needs unit test coverage and the tests must follow this repo's exact xUnit/Moq/Shouldly naming and placement conventions.
---

<!-- GENERATED from .agent/ by scripts/sync-agent-harness.ps1 - do not edit directly -->
# Create Unit Test

Generate xUnit tests using Moq and Shouldly, following this repo's exact naming convention and
placement. For new/missing coverage or bringing existing tests into line — not for integration
tests (beyond an in-memory `DbContext`) or testing non-public members (test through the public API).

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| Target class | Yes | The C# class needing tests. |
| Test project path | No | Path to the `*.Tests` project, if it needs locating. |

## Workflow

1. **Analyze the target:** namespace, name, public methods, constructor-injected interfaces,
   whether it depends on a `DbContext` (use an in-memory database for those — no Repository
   pattern in this repo).
2. **Locate/create the test file** in `[OriginalProjectName].Tests`, mirroring the source folder
   structure, file-scoped namespace `[OriginalNamespace].Tests`.
3. **Usings:** `Moq`, `Xunit`, `Shouldly`.
4. **Scaffold:** class `[TargetClassName]Tests`; a `Mock<T>` field per injected interface; set up
   the SUT with mocked dependencies.
5. **Test methods** for every public method, named **exactly**
   `[Method name]_When_[Condition]_Then_[Result]`
   (e.g. `CalculateTotal_When_CartIsEmpty_Then_ReturnsZero`).
6. **Assertions:** Shouldly only — `result.ShouldBe(expected)`, `action.ShouldThrow<T>()`. Never
   `Assert.X`.

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Wrong test name shape | Exactly `[Method]_When_[Condition]_Then_[Result]`. |
| `Assert.X` instead of Shouldly | Convert to the fluent equivalent. |
| Block-scoped namespace | Use file-scoped `namespace X;`. |
| Missed constructor dependency | Scan every constructor; mock every injected interface. |
