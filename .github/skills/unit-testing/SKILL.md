---
name: unit-testing
description: Write unit tests following team conventions for naming, structure, and assertions. Use this skill when asked to write tests, add unit tests, or test a method.
---

## Process

1. Analyze the code to understand:
    - What class/method is being tested
    - What dependencies need mocking
    - What are the meaningful test cases (happy path, edge cases, errors)
2. Generate tests following the conventions found below
3. Each test should be focused on one behavior

## Conventions
- Write unit tests for all public APIs
- Use xUnit as the testing framework and specifically xunit v3 using the latest Microsoft Test Platform version
- Follow AAA pattern (Arrange, Act, Assert)
- Always comment in a test the "Arrange.", "Act.", and "Assert." sections
- Use meaningful test names (e.g., `AddMessageAsync_GivenSomeLargeContent_ShouldStoreInBlob`)
- Mock external dependencies with Moq
- Aim for high code coverage on business logic
- Each method being tested should have its own class so xUnit can run tests in parallel
- Each method being tested should have at least one test for success and one for failure

## Test Structure

- Each method being tested should have its own test class placed under `UnitTests|IntegrationTests/<SutName>Tests/<MethodName>Tests`, with the corresponding namespace `<RootNamespace>.Tests.UnitTests|IntegrationTests.<SutName>Tests` and class name `<MethodName>Tests`
- Methods are named using the pattern: `MethodUnderTesting_GivenSomeCondition_ShouldSomeExpectedResult` 

```csharp
// Example class and method to be tested

namespace MyProject.Services;

public class UserService
{
    public async Task<int> SaveUserAsync(User user)
    {
        // Implementation goes here
    }
}

.....

// Example test class and method following the conventions
// Folder: UnitTests/UserServiceTests/SaveUserAsyncTests.cs

namespace MyProject.Tests.UnitTests.UserServiceTests;

public class SaveUserAsyncTests
{
    [Fact]
    public async Task SaveUserAsync_GivenAValidUser_ShouldSaveSuccessfully()
    {
        // Arrange.
        var service = new UserService();
        var user = new User();

        // Act.
        var userId = await service.SaveUserAsync(user);

        // Assert.
        // Add assertions to verify that the user was saved successfully using Shouldly nuget library.
        userId.ShouldBeGreaterThan(0);
    }
}
```

## Running tests
- xUnit v3 using the latest Microsoft Test Platform runs tests from the CLI via `dotnet run <path-to-test-project>`.
- when running tests, just run the tests added/modified, not the entire suite
