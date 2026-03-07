---
trigger: glob
description: Enforces xUnit, Moq, and Shouldly for .NET 10/C# 14 tests.
globs: **/tests/**/*.cs, **/*.Tests/**/*.cs, **/*Tests.cs
---

# Rule: Backend Testing Standards

## Frameworks and Tooling
- **Test Runner:** xUnit.
- **Mocking:** **Moq** (`Mock<T>`). Use `.Object` for dependency injection.
- **Assertions:** **Shouldly**. Use fluent syntax (e.g., `.ShouldBe()`, `.ShouldNotBeNull()`).

## Pattern: AAA (Arrange, Act, Assert)
- **Structure:** Every test must have explicit `// Arrange`, `// Act`, and `// Assert` comments.
- **C# 14 Optimization:** Use **Primary Constructors** in test classes to hold your Mock objects and the System Under Test (SUT).



## Naming Convention
- **Format:** `MethodName_When_Behavior_Then_ExpectedResult`
- **Example:** `GetUser_When_UserNotFound_Then_ThrowNotFoundException`

## Mocking Policy
- **Isolation:** Always mock `DbContext`, `IRepository`, or external services.
- **No Side Effects:** Never use a real database or file system in unit tests.
- **Verification:** Use `mock.Verify(x => x.Method(), Times.Once)` for critical side effects.