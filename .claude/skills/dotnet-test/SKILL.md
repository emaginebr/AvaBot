---
name: dotnet-test
description: "Manages unit tests using xUnit in .NET projects. Creates the test project if it doesn't exist, organizes tests by layer (Domain, Application, Infra, API), mirrors the source folder structure, and generates test files. Use when creating, updating, or organizing unit tests."
allowed-tools: Read, Grep, Glob, Bash, Write, Edit, Task
user-invocable: true
---

# .NET Unit Test Manager (xUnit)

You are an expert assistant that helps developers create and manage unit tests using xUnit in .NET projects. You follow a strict convention for project structure, naming, and organization.

## Input

The user will describe what to test: `$ARGUMENTS`

Before generating tests:
1. **Read the solution structure** — Identify all projects/layers and the `.sln` file
2. **Find the test project** — Look for a project ending in `.Tests` (e.g., `MyApp.Tests`)
3. **Read the class to be tested** — Understand its dependencies, methods, and behavior
4. **Read the DI/Startup** — Understand how services are wired up
5. **Identify existing test patterns** — If tests already exist, match their style exactly

---

## Test Project Convention

### Single Test Project

Create **only one** test project for the entire solution, named `{SolutionName}.Tests`.

```
MySolution/
├── MySolution.sln
├── MySolution.API/
├── MySolution.Application/
├── MySolution.Domain/
├── MySolution.Infra/
├── MySolution.Infra.Interfaces/
├── MySolution.DTO/
└── MySolution.Tests/            ← Single test project
    ├── MySolution.Tests.csproj
    ├── Domain/
    │   ├── Services/
    │   │   ├── ProductServiceTest.cs
    │   │   └── OrderServiceTest.cs
    │   └── Mappers/
    │       └── ProductMapperTest.cs
    ├── Application/
    │   └── StartupTest.cs
    ├── Infra/
    │   ├── Repository/
    │   │   └── ProductRepositoryTest.cs
    │   └── Mappers/
    │       └── ProductDbMapperTest.cs
    └── API/
        └── Controllers/
            └── ProductControllerTest.cs
```

### Folder Mirroring Rules

- The test project mirrors the **layer names** as top-level folders: `Domain/`, `Application/`, `Infra/`, `API/`, etc.
- Inside each layer folder, mirror the **subfolder structure** of the source project (e.g., `Services/`, `Repository/`, `Mappers/`, `Controllers/`)
- Each test file corresponds to **one source class**
- Test file name = `{ClassName}Test.cs` (e.g., `ProductService` → `ProductServiceTest.cs`)

---

## Creating the Test Project

If the test project does not exist, create it:

```bash
# 1. Create the xUnit project
dotnet new xunit -n {SolutionName}.Tests -o {SolutionName}.Tests

# 2. Add it to the solution
dotnet sln {SolutionName}.sln add {SolutionName}.Tests/{SolutionName}.Tests.csproj

# 3. Add references to all projects that need testing
dotnet add {SolutionName}.Tests/{SolutionName}.Tests.csproj reference {SolutionName}.Domain/{SolutionName}.Domain.csproj
dotnet add {SolutionName}.Tests/{SolutionName}.Tests.csproj reference {SolutionName}.Infra/{SolutionName}.Infra.csproj
dotnet add {SolutionName}.Tests/{SolutionName}.Tests.csproj reference {SolutionName}.Application/{SolutionName}.Application.csproj
dotnet add {SolutionName}.Tests/{SolutionName}.Tests.csproj reference {SolutionName}.API/{SolutionName}.API.csproj
# ... add all projects that contain classes to test

# 4. Add Moq for mocking
dotnet add {SolutionName}.Tests/{SolutionName}.Tests.csproj package Moq

# 5. Delete the default UnitTest1.cs
rm {SolutionName}.Tests/UnitTest1.cs
```

### Required Packages

The test project must have:
- `xunit` (included by template)
- `xunit.runner.visualstudio` (included by template)
- `Microsoft.NET.Test.Sdk` (included by template)
- `Moq` (add manually)

---

## Test File Structure

### Naming Conventions

- **Class name**: `{ClassName}Test` (e.g., `ProductServiceTest`)
- **Namespace**: `{SolutionName}.Tests.{Layer}.{SubFolder}` (e.g., `MyApp.Tests.Domain.Services`)
- **Method name**: `{MethodName}_Should{ExpectedBehavior}_When{Condition}` (e.g., `InsertAsync_ShouldThrowException_WhenNameIsEmpty`)

### Template

```csharp
using Xunit;
using Moq;
// ... other usings

namespace {SolutionName}.Tests.{Layer}.{SubFolder}
{
    public class {ClassName}Test
    {
        private readonly Mock<IDependency1> _dependency1Mock;
        private readonly Mock<IDependency2> _dependency2Mock;
        private readonly {ClassName} _sut; // System Under Test

        public {ClassName}Test()
        {
            _dependency1Mock = new Mock<IDependency1>();
            _dependency2Mock = new Mock<IDependency2>();
            _sut = new {ClassName}(
                _dependency1Mock.Object,
                _dependency2Mock.Object
            );
        }

        [Fact]
        public async Task {MethodName}_Should{Expected}_When{Condition}()
        {
            // Arrange
            // ...

            // Act
            // ...

            // Assert
            // ...
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("valid", true)]
        public async Task {MethodName}_ShouldValidate_When{Condition}(string input, bool expected)
        {
            // Arrange, Act, Assert
        }
    }
}
```

---

## What to Test

### Services (Domain Layer)
- **Validation logic**: Empty/null inputs, invalid values, boundary conditions
- **Business rules**: Authorization checks, ownership validation, state transitions
- **Happy path**: Successful operations with correct data
- **Exception handling**: Expected exceptions for error scenarios
- **Mock dependencies**: All repository and external service calls

### Mappers
- **All properties mapped correctly**: Verify each field in the output matches the input
- **Null/default handling**: Nullable fields, default values
- **No mocking needed**: Mappers are pure functions, test with real data

### Repositories (Infra Layer)
- Only test if there is **custom logic** (e.g., complex queries, computed values)
- Use **in-memory database** (`UseInMemoryDatabase`) for EF Core repository tests
- Do NOT test basic CRUD that EF Core already guarantees

### Controllers (API Layer)
- **Authorization**: Verify `Unauthorized()` when no session
- **Not found**: Verify `NotFound()` for missing resources
- **Forbidden**: Verify `Forbid()` for access denied
- **Happy path**: Verify `Ok()` with correct data
- **Mock services**: All service layer calls

---

## Running Tests

```bash
# Run all tests
dotnet test {SolutionName}.Tests

# Run with verbose output
dotnet test {SolutionName}.Tests --verbosity normal

# Run specific test class
dotnet test {SolutionName}.Tests --filter "FullyQualifiedName~ProductServiceTest"

# Run specific test method
dotnet test {SolutionName}.Tests --filter "FullyQualifiedName~InsertAsync_ShouldThrowException_WhenNameIsEmpty"
```

---

## Checklist

Before finishing, verify:
- [ ] Test project exists and is added to the solution
- [ ] Folder structure mirrors the source project layers
- [ ] Each test file tests only one source class
- [ ] Test file names end with `Test.cs`
- [ ] All dependencies are mocked (no real DB, no real HTTP calls)
- [ ] Tests follow Arrange/Act/Assert pattern
- [ ] Method names describe behavior: `{Method}_Should{What}_When{Condition}`
- [ ] Tests compile: `dotnet build {SolutionName}.Tests`
- [ ] Tests pass: `dotnet test {SolutionName}.Tests`
