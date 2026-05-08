# .NET And C# Rules

Load this file when editing or reviewing C#/.NET code.

## Style

- Use file-scoped namespaces.
- Seal classes by default unless inheritance is explicitly intended.
- Prefer primary constructors for dependency injection.
- Use `var` for local variables.
- Use collection expressions (`[a, b]`) and raw string literals for SQL/multiline strings.
- Constants use `SCREAMING_SNAKE_CASE`; private fields use `_camelCase`; public members use `PascalCase`.
- Public interface members need XML documentation.

## Structure

- Prefer feature-based organization over technical-layer organization when the project already follows that pattern.
- Keep request handlers thin; move business logic to services/domain code.
- Keep repositories focused on data access only.
- Keep `Startup.cs` thin; delegate feature registrations to feature `Extensions`.
- Put cross-cutting infrastructure in `Infrastructure/`.

## API And Contracts

- Use the project’s established request handler base classes for public and internal endpoints.
- Validate all public request input and throw `ContractValidationException` for invalid contracts.
- Use sealed records with positional parameters for client or third-party response DTOs.
- For serialized contracts, keep field ordering/renames backward compatible where existing consumers or persisted data depend on them.

## Exceptions

- Use specific exceptions: `ContractValidationException`, `AccessDeniedException`, `NotFoundException`, or `CustomWebException`.
- Do not use exceptions for normal control flow.
- Use typed/custom web exceptions when client logic depends on the error type.

## Review Checks

- Missing authorization or validation on public handlers.
- Business logic inside handlers or repositories.
- Unsealed classes without a reason.
- Missing interface XML docs.
- Direct `new HttpClient()` instead of `IHttpClientFactory` or platform client abstractions.
- Scoped dependencies injected into singletons.
- Service locator usage in business code.
