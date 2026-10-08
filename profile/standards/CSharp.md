# C# Standards

## Purpose

Define default coding standards for C# code across services, applications, libraries, and tests.

## Baseline

This standard starts from Microsoft's published .NET and C# conventions, then adds team-level guidance where Microsoft leaves room for local choice.

### Microsoft Published Baseline

- `.NET coding conventions` on Microsoft Learn
- `Common C# code conventions` on Microsoft Learn
- `.editorconfig` conventions used in Microsoft-maintained .NET repositories

## Core Principles

- Follow standard .NET naming and layout conventions first.
- Write clear, readable code before clever code.
- Prefer explicit domain language over vague utility naming.
- Use composition and dependency injection instead of hidden global state.
- Keep code easy to scan in the same way Microsoft code samples and product repos are easy to scan.

## Naming

- Use `PascalCase` for classes, records, enums, interfaces, methods, properties, and public constants.
- Prefix interfaces with `I`.
- Use `camelCase` for local variables and method parameters.
- Use meaningful nouns for types and verbs for methods.
- Use meaningful names that describe intent, not implementation detail.
- Name async methods with the `Async` suffix.

## File Organization

- Keep exactly one declared type per C# source file. This includes classes, interfaces, records, structs, enums, and delegates; do not combine related contract types, request types, or supporting types into a single file.
- Match the file name to its sole type name.
- Organize types under their feature or bounded domain, then use purpose-based folders when they improve discoverability: `Services`, `Requests`, `Handlers`, `Commands`, `Queries`, `Interfaces`, `Contracts`, `Models`, and `Enums` are common examples.
- Do not use a single broad `Contracts` or other catch-all file to collect unrelated public API types. Each contract and model remains a separately named file in the appropriate purpose-based folder.
- Preserve framework- or tool-generated source as generated; do not use generated code as a reason to combine hand-authored types.
- Keep tests in a separate test project mirroring the production structure.

## Style

- Use file-scoped namespaces unless a block-scoped namespace is required.
- Use four spaces for indentation.
- Open braces stay on their own line for type and member declarations.
- Keep `using` directives at the top of the file.
- Sort `System` namespaces first.
- Prefer constructor injection for dependencies.
- Prefer expression-bodied members only when they improve readability.
- Use `var` when the type is obvious from the right-hand side; otherwise prefer explicit types.
- Keep nullable reference types enabled.
- Avoid deeply nested conditionals; return early when possible.
- Prefer language keywords like `string`, `int`, and `bool` over framework type names in declarations where standard .NET style does the same.

## Layout

- Order file contents so the public surface is easy to find quickly.
- Keep related members together.
- Prefer shorter methods with one clear job.
- Extract private helper methods when a method starts mixing multiple concerns.
- Avoid regions unless they solve a real readability problem in an otherwise large, legacy file.

## Design Guidance

- Keep domain logic out of controllers and UI concerns.
- Use records for immutable value-focused data shapes where they fit naturally.
- Prefer small service abstractions over static helper classes.
- Validate inputs close to the application boundary.
- Keep side effects explicit and easy to trace.
- Prefer dependency inversion at boundaries instead of reaching into static infrastructure from domain code.
- Follow existing .NET framework patterns before inventing custom patterns.

## Error Handling

- Do not swallow exceptions.
- Throw specific exceptions when the caller can act on the distinction.
- Use guard clauses for invalid input.
- Log enough context to diagnose failures without leaking secrets.

## Testing

- Write unit tests for domain and application logic.
- Keep tests deterministic and isolated.
- Use descriptive test names that state behavior.
- Prefer testing observable behavior over implementation details.
- After a test performs an action, assert the resulting state or output that proves the action actually worked.
- Do not stop at "request succeeded" or "button clicked" style checks when the code under test is expected to change persisted data, UI state, navigation state, or emitted output.
- Run the relevant automated tests for the code you changed before handing work back when the environment supports it.

## Avoid

- God classes and catch-all utility files.
- Hidden static state.
- Long methods with multiple responsibilities.
- Boolean parameter overloads that make call sites unclear.

## Notes

- When a repository already contains an `.editorconfig`, analyzer rules, or SDK-specific conventions, those repository rules take precedence over this document.
