---
name: create-unit-test
description: Scaffolds xUnit tests using Moq and Shouldly, adhering strictly to \[Method name\]_When_\[Condition\]_Then_\[Result\] naming conventions and in-memory databases.
---

# Create Unit Test

This skill helps you generate highly structured unit tests for C# codebases. It ensures all tests adhere to specific standards including file-scoped namespaces, xUnit, Moq, and Shouldly's fluent syntax, as well as an exact naming convention for test methods.

## When to Use

- When writing tests for a newly created C# class or method.
- When generating missing test coverage for existing public methods.
- When refactoring existing tests to conform to the project's testing conventions.

## When Not to Use

- When writing integration tests (unless testing data contexts with an in-memory database).
- When attempting to explicitly test private, internal, or protected methods (test behaviors through public methods).

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| Target class | Yes | Information or file content for the C# class needing tests. |
| Test project path | No | Path to the `*.Tests` project if it needs to be located manually. |

## Workflow

### Step 1: Analyze the target class
1. Identify the class namespace, name, and public methods.
2. Identify all interfaces injected via the constructor.
3. Identify if the class depends on a database context that requires an in-memory database configuration.

### Step 2: Set up the test file
1. Locate or create the corresponding `*.Tests` project (typically `[OriginalProjectName].Tests`).
2. Create folders matching the source structure, ensuring the test file is placed in the exact corresponding directory inside the test project.
3. Use file-scoped namespaces for the test class. Ensure the namespace matches the test project (e.g., `[OriginalNamespace].Tests`).

### Step 3: Add required usings
Add the following `using` statements at the top of the file:
- `using Moq;`
- `using Xunit;`
- `using Shouldly;`

### Step 4: Scaffold the test class
1. Create a public test class named `[TargetClassName]Tests`.
2. Generate a `Mock<T>` field for every interface found in the constructor of the target class.
3. Use an in-memory database for tests if a data context or repository pattern is relied upon.
4. Set up initialization logic (constructor or setup method) to instantiate the System Under Test (SUT) with the mocked dependencies.

### Step 5: Generate test methods
For all public methods in the target class, generate robust test cases. 

**CRITICAL**: You must strictly apply the following naming convention:
`[Method name]_When_[Condition]_Then_[Result]`

Example:
`public void CalculateTotal_When_CartIsEmpty_Then_ReturnsZero()`

### Step 6: Write assertions
Use Shouldly's fluent syntax for all assertions.
- Good: `result.ShouldBe(expected);` 
- Good: `action.ShouldThrow<ArgumentNullException>();`
- Bad: `Assert.Equal(expected, result);`

## Validation

- [ ] The generated test file is physically placed in the correct `*.Tests` project.
- [ ] The test file uses file-scoped namespaces.
- [ ] Required using directives (`Moq`, `Xunit`, `Shouldly`) are present.
- [ ] A `Mock<T>` is created for every injected interface.
- [ ] An in-memory database is properly configured if applicable.
- [ ] All public methods in the target class have tests.
- [ ] Test names strictly follow the `[Method name]_When_[Condition]_Then_[Result]` convention.
- [ ] All assertions use Shouldly's fluent syntax (`ShouldBe`, `ShouldThrow`, etc.).

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Ignoring the test naming convention | Use the exact format: `[Method name]_When_[Condition]_Then_[Result]`. |
| Writing standard assertions | Convert all `Assert.X` methods to their `Shouldly` equivalents (e.g., `result.ShouldBe(expected)`). |
| Forgetting file-scoped namespaces | Convert block-scoped `namespace X { }` to `namespace X;` |
| Overlooking injected dependencies | Scan the class constructors thoroughly and mock every single injected interface using `Mock<interface>`. |
