---
paths:
  - "tests/**/*.cs"
---

# Backend testing standards

- **Stack:** xUnit, Moq (`Mock<T>`, pass `.Object`) and Shouldly (`ShouldBe()`, `ShouldNotBeNull()`, `Should.Throw<T>(...)`). Never xUnit `Assert` calls, MSTest or NUnit.
- **Naming:** `MethodName_When_Behavior_Then_ExpectedResult`, e.g. `GetUser_When_UserNotFound_Then_ThrowNotFoundException`.
- **Structure:** AAA, with explicit `// Arrange`, `// Act`, `// Assert` comments. Primary constructors may hold the mocks and the SUT.
- **Placement:** `tests/Services/<Service>.Tests/`, mirroring the source folders, with file-scoped namespace `<SourceNamespace>.Tests`.
- **Isolation:**
  - Mock external services and injected interfaces.
  - For a `DbContext`, use the EF Core InMemory provider (`UseInMemoryDatabase`) or `MockQueryable.Moq`, as the test project already does.
  - Never use a real database, network or file system.
- **Verification:** `Verify(x => x.Method(), Times.Once)` on critical side effects.
- **Run:** `dotnet test tests/Services/<Service>.Tests/<Service>.Tests.csproj --filter "FullyQualifiedName~<Class>"`.
