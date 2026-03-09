---
name: generate-dotnet-test
description: Generates a complete xUnit test file for a given .NET class using Moq and Shouldly.
---

# Logic
1. **Analyze Source:** Read the target `.cs` file to identify public methods and dependencies.
2. **Mock Dependencies:** Create `Mock<T>` for every interface found in the constructor.
3. **Setup Shouldly:** Use Shouldly's fluent syntax for all assertions.
4. **Project Check:** Ensure the test is placed in the correct `*.Tests` project and namespace.
5. **Database:** Use in-memory database for tests.
6. **Test Cases:** Create test cases for all public methods.
7. **Test Naming:** Use the following naming convention: `[Method name]_When_[Condition]_Then_[Result]`.

# Template Format
- Use file-scoped namespaces.
- Include `using Moq;`, `using xUnit;`, and `using Shouldly;`.