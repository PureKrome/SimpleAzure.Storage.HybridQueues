# GitHub Copilot Instructions

## Project Context

SimpleAzure.Storage.HybridQueues is a .NET library focused on making Azure Storage Queue usage simple and
resilient when message payloads exceed queue size limits. It provides a hybrid queue pattern that 
automatically stores oversized payloads in Azure Blob Storage, keeps a queue-safe reference in Azure Queue Storage,
and transparently handles retrieval and cleanup so consumers can work with queue messages without manually managing blob fallback logic.

## Code Style & Standards

### General .NET Guidelines
- Use modern C# features (pattern matching, records, nullable reference types, file-scoped namespaces)
- Follow Microsoft's C# coding conventions (enforced via `.editorconfig`)
- Prefer `async`/`await` for I/O operations
- Use meaningful, descriptive names (avoid abbreviations unless widely known)
- All comments should be in English and finish with a period

### Code Organization
- Use file-scoped namespaces
- One class per file (match filename to class name)
- Group using statements (System namespaces first, then third-party, then local)
- Order class members: fields, constructors, properties, methods (public before private)

### Error Handling
- Use exceptions for exceptional cases, not control flow
- Provide meaningful exception messages with context
- Use guard clauses for parameter validation
- Consider custom exceptions for domain-specific errors
- Always validate input parameters (use ArgumentNullException, ArgumentException)

### Async/Await
- Always use `ConfigureAwait(false)` in library code (not in ASP.NET Core endpoints)
- Pass `CancellationToken` to all async methods
- Don't use `.Result` or `.Wait()` - always await
- Return `Task` or `Task<T>`, not `async void` (except event handlers)

### Logging
- Use structured logging with ILogger
- Include relevant context in log messages
- Use appropriate log levels (Trace, Debug, Information, Warning, Error, Critical)  
- Log at method entry/exit for important operations
- Include correlation IDs where applicable

### Testing
- Refer to the .github/skills/unit-testing/SKILL.md for testing guidelines

### Documentation
- Use XML documentation comments for public APIs
- Include `<summary>`, `<param>`, `<returns>`, and `<exception>` tags
- Provide code examples in remarks when helpful
- Keep README.md updated with examples
- Document breaking changes in release notes

### Performance
- Avoid allocations in hot paths
- Use `Span<T>`, `Memory<T>`, and `ReadOnlySpan<T>` where appropriate
- Consider object pooling for frequently allocated objects
- Profile before optimizing
- Use `ValueTask<T>` for high-performance scenarios

### Dependencies
- Minimize external dependencies
- Use well-maintained, popular NuGet packages
- Keep packages up-to-date
- Prefer Microsoft packages for common functionality
- Review package licenses for compatibility

### Design Patterns
- Favor composition over inheritance
- Use dependency injection
- Apply SOLID principles
- Keep classes focused (Single Responsibility Principle)
- Program to interfaces, not implementations
- Use builder pattern for complex object construction

### Records and Data Types
- Use `record` for DTOs and immutable data
- Use `readonly struct` for small, immutable value types
- Prefer immutability where possible
- Use nullable reference types (`?`) appropriately

### API Design
- Keep public API surface small and focused
- Make it hard to use incorrectly (pit of success)
- Provide async methods for I/O operations
- Accept interfaces in constructors, return concrete types
- Version APIs carefully to avoid breaking changes

## What NOT to Do

- ❌ Don't ignore CA (Code Analysis) warnings without justification
- ❌ Don't commit commented-out code
- ❌ Don't use magic numbers/strings (use constants)
- ❌ Don't catch generic exceptions without rethrowing
- ❌ Don't use `Thread.Sleep` in async code (use `Task.Delay`)
- ❌ Don't expose internal implementation details in public APIs
- ❌ Don't write methods longer than ~50 lines (refactor into smaller methods)
- ❌ Don't use regions to hide code
- ❌ Don't use dynamic types
- ❌ Don't use static classes for stateful operations
- ❌ Don't use Automapper or MediatR

## Code Review Checklist

When reviewing code suggestions, ensure:
- [ ] Code follows project conventions
- [ ] All public APIs have XML documentation
- [ ] Async methods use `CancellationToken`
- [ ] Proper exception handling is in place
- [ ] Tests are included for new functionality
- [ ] No hardcoded values (use configuration)
- [ ] Logging is appropriate and structured
- [ ] Performance implications are considered
- [ ] Breaking changes are documented

## Additional Context

- This project prioritizes **simplicity** and **developer experience**
- Code should be self-documenting where possible
- Favor clarity over cleverness
- Consider the library consumer's perspective
- Maintain backward compatibility when possible
