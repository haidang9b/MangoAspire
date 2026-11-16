---
name: create-unit-test
description: Use when a C# class or method needs unit test coverage and the tests must follow this repo's exact xUnit/Moq/Shouldly naming and placement conventions.
---

# Create Unit Test

Generate xUnit tests using Moq and Shouldly, following this repo's exact naming convention and
placement. Use it for new or missing coverage, or to bring existing tests into line. Not for
integration tests (beyond an in-memory `DbContext`) or for testing non-public members (test through
the public API).

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| Target class | Yes | The C# class to test. If none was named, ask: "Which handler, service or validator should I test?" |
| Test project path | No | The `*.Tests` project, if it needs locating (under `tests/`). |

## Workflow

1. **Analyze the target:** namespace, name, public methods, constructor-injected interfaces, and
   whether it depends on a `DbContext` (use an in-memory database for those; there is no repository
   layer in this repo).
2. **Locate or create the test file** in `[OriginalProjectName].Tests`, mirroring the source folder
   structure, with file-scoped namespace `[OriginalNamespace].Tests`.
3. **Usings:** `Moq`, `Xunit`, `Shouldly`.
4. **Scaffold:** class `[TargetClassName]Tests`, a `Mock<T>` field per injected interface, and the
   SUT built with the mocked dependencies.
5. **Test methods** for every public method, named **exactly**
   `[Method name]_When_[Condition]_Then_[Result]`, in AAA layout. Verify critical side effects with
   Moq `Verify(...)`.
6. **Assertions:** Shouldly only (`result.ShouldBe(expected)`, `Should.Throw<T>(...)`), never
   xUnit `Assert` calls.
7. **Run them:** `dotnet test <TestProject>.csproj --filter "FullyQualifiedName~<TargetClassName>Tests"`.
   They must pass before you report.
8. **Report** the test file path, the test names and the run result. When the user asked for a draft
   only, show the code and ask before saving it.

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Wrong test name shape | Exactly `[Method]_When_[Condition]_Then_[Result]`. |
| xUnit `Assert` instead of Shouldly | Convert to the fluent equivalent. |
| Block-scoped namespace | Use file-scoped `namespace X;`. |
| Missed constructor dependency | Scan every constructor; mock every injected interface. |
| Reporting without running | Run the filtered `dotnet test` first. |
