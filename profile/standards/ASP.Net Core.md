# ASP.NET Core Standards

## Purpose

Define ASP.NET Core specific standards that build on the shared C# standards.

## Scope

Apply this standard together with [C#](./CSharp.md) for ASP.NET Core applications and services.

## Baseline

This standard starts from Microsoft's published ASP.NET Core guidance and the conventions visible in Microsoft-maintained ASP.NET Core repositories, then adds local architectural defaults where needed.

### Microsoft Published Baseline

- ASP.NET Core fundamentals and architecture guidance on Microsoft Learn
- Conventions used in the `dotnet/aspnetcore` repository
- Shared .NET code style and analyzer conventions used by Microsoft repos

## Core Principles

- Follow built-in ASP.NET Core conventions before introducing custom framework patterns.
- Keep framework wiring separate from application and domain logic.
- Use dependency injection intentionally and keep registrations understandable.
- Favor explicit request pipelines and predictable service boundaries.
- Follow framework conventions unless there is a strong reason not to.

## Project Structure

- Use clear project and folder boundaries that make request flow easy to trace.
- Organize by feature or bounded domain where practical.
- Keep web, application, and infrastructure concerns clearly separated.
- Keep startup and composition-root code easy to scan.
- Keep shared abstractions small and purposeful.
- Prefer layouts that let a reader quickly find endpoints, requests, handlers, domain logic, and persistence logic.

## HTTP Layer

- Keep controllers and endpoints thin.
- Validate request models at the boundary.
- Return clear, intentional response shapes.
- Avoid leaking persistence models directly through public APIs unless that is an intentional design choice.
- Prefer built-in model binding, validation, filters, and results before custom plumbing.

## Dependency Injection

- Prefer constructor injection.
- Register services with the narrowest practical lifetime.
- Avoid service locators and ambient state.
- Keep registration code grouped and readable.
- Use extension methods to organize larger registration blocks when startup code begins to sprawl.

## Configuration and Options

- Use strongly typed options for structured configuration.
- Validate critical configuration at startup when practical.
- Keep secrets out of source control and logs.
- Keep environment-specific behavior explicit and discoverable.

## Data Access

- Keep EF Core queries intentional and readable.
- Avoid putting business workflow logic into controllers or DbContext configuration.
- Be explicit about tracking behavior, includes, and transaction boundaries when they matter.
- Keep persistence concerns behind application-facing services or handlers when that separation improves clarity.

## Middleware and Pipelines

- Use middleware for cross-cutting concerns, not feature-specific business rules.
- Keep middleware focused and composable.
- Make authentication, authorization, and exception handling paths easy to trace.
- Prefer the default hosting and middleware model used by current ASP.NET Core templates unless there is a clear benefit to diverging.

## Layout

- Keep `Program.cs` small enough to scan quickly.
- Move repeated registration and configuration code into extension methods or dedicated setup modules.
- Keep endpoint definitions close to their request and response contracts where practical.
- Keep hosting, HTTP, application, and infrastructure concerns from collapsing into one file or one project area.

## Testing

- Write unit tests for domain and application logic.
- Write integration tests for API and pipeline behavior.
- Use WebApplicationFactory or equivalent tooling where it improves confidence.
- Keep tests deterministic and easy to understand.
- For UI, integration, and end-to-end tests, assert the page or response state after each meaningful user action instead of only checking that the action completed.
- When a form save, navigation, mutation, or authorization flow is under test, verify the resulting rendered values, route, persisted data, or visible state that proves the outcome.
- When persistence matters, verify the rendered state again after reload or revisit rather than stopping at the immediate post-save state.
- For UI-affecting changes, capture and review screenshots as part of validation.
- End every coding cycle with a final screenshot review of the affected UI before handoff.
- After ASP.NET Core code changes, run the relevant automated tests as part of validation, not only a build.

## Avoid

- Controllers that orchestrate every layer directly.
- Large service classes with mixed responsibilities.
- Static access to scoped request data.
- Hidden behavior in startup code that is hard to discover.

## Notes

- For ASP.NET Core projects already aligned to Microsoft templates or an existing solution architecture, preserve the established approach unless there is a strong payoff to refactor it.
