## General

- Make only high confidence suggestions when reviewing code changes.
- Never change global.json unless explicitly asked to.
- Use conventional commits.

## C# Guidelines

### Formatting

- Use the latest C# 13 features.
- Apply code-formatting style defined in `.editorconfig`.
- Prefer file-scoped namespace declarations and single-line using directives.
- Insert a newline before the opening curly brace of any code block (e.g., after `if`, `for`, `while`, `foreach`, `using`, `try`, etc.).
- Ensure that the final return statement of a method is on its own line.
- Use pattern matching and switch expressions wherever possible.
- Use `nameof` instead of string literals when referring to member names.
- Ensure that XML doc comments are created for any public APIs. When applicable, include `<example>` and `<code>` documentation in the comments.

### Nullable Reference Types

- Declare variables non-nullable, and check for `null` at entry points.
- Always use `is null` or `is not null` instead of `== null` or `!= null`.
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.

### Asynchronous Code

- Always use the `await` keyword on async operations to ensure exceptions are captured and to avoid blocking the calling thread.
- Avoid `async void`.
- Name Methods with the "Async" Suffix.
- Allow cancellation by accepting a `CancellationToken` in async methods.
- Use ConfigureAwait(false) When Appropriate. In library code or server-side processes, use `ConfigureAwait(false)` to avoid unnecessary context switches and potential deadlocks.
- Keep Async Code “Async All the Way”. Propagate async all the way from the entry point (like event handlers or controller actions) rather than mixing sync and async code.

### Testing

- Emit "Act", "Arrange" or "Assert" comments.
- We use xUnit for tests.
- Use NSubstitute for mocking in tests.
- Copy existing style in nearby files for test method names and capitalization.
