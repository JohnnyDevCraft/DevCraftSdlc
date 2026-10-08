# .NET Mediator Pattern with XMediat

## Purpose

Define how .NET applications use the mediator pattern through Xelseor LLC's `XMediat` library. The mediator is the in-process boundary between callers and application behavior: callers express intent as messages, while XMediat locates and invokes the matching handlers.

XMediat is independently implemented and unaffiliated with MediatR. It is not source-compatible, binary-compatible, or a drop-in replacement. Use XMediat's actual contracts and behavior rather than assuming MediatR conventions apply.

## When To Use

- Use the mediator pattern to keep endpoints, user interfaces, hosted services, and other callers independent of concrete application handlers.
- Use it when commands, queries, requests, notifications, events, or streams benefit from consistent dispatch, cancellation, validation, lifecycle, or pipeline behavior.
- Use it to make module entry points explicit in a modular monolith.
- Do not use it to hide arbitrary service calls or replace clear domain collaboration inside a cohesive component.
- Do not treat in-process publication as durable messaging. External brokers, retries, delivery guarantees, and outbox behavior remain separate integration concerns.

## Core Principles

- Messages describe intent or facts; handlers perform application behavior.
- Define messages in contract or application-abstraction projects and handlers in implementation projects.
- Prefer one handler responsibility per type and one hand-authored type per file.
- Select the message family whose semantics match the operation.
- Inject the narrow dispatcher or publisher interface a consumer needs.
- Keep handlers thin enough to coordinate domain services, repositories, and integrations without becoming unstructured transaction scripts.
- Propagate the caller's `CancellationToken` through dispatch and every asynchronous dependency.
- Keep transport concerns such as HTTP status codes, controllers, endpoints, and UI models outside handlers and shared message contracts.

## XMediat Packages

| Package | Use |
|---|---|
| `XMediat.Contracts` | Dependency-light message markers, `Unit`, command and query response envelopes, paging, and sorting metadata. Reference it from projects that define or share messages. |
| `XMediat` | Handler interfaces, dispatcher and publisher APIs, Microsoft dependency injection, assembly scanning, publication strategies, lifecycle hooks, continuation pipelines, and request processors. Reference it from projects that implement handlers or dispatch messages. |

The runtime package references the matching contracts version transitively. Do not reference `XMediat` from a contracts-only project.

Reference the package that expresses each project's architectural responsibility directly. A project that declares XMediat messages must directly reference `XMediat.Contracts`; a project that implements handlers or dispatches messages must directly reference `XMediat`. Do not hide either dependency behind a transitive project reference.

XMediat is proprietary. Every human who develops, builds, debugs, tests, deploys, or redistributes an application containing it requires an authorized named developer seat. Noninteractive organization-bound build agents follow the license's CI exception. Do not redistribute XMediat as a standalone DLL, NuGet package, SDK, framework, component, source package, or symbol package.

The license bundled with the current prerelease is a business draft pending qualified-counsel approval. Do not treat this architecture guide as authorization for external distribution.

## Azure Artifacts Source

Consume XMediat from the private `XelseorPackages` Azure Artifacts feed. Place this `NuGet.config` beside the solution or consuming project:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="XelseorPackages" value="https://pkgs.dev.azure.com/xelseor/_packaging/XelseorPackages/nuget/v3/index.json" />
  </packageSources>
</configuration>
```

Because `<clear />` removes inherited package sources, verify that this feed supplies all required dependencies through packages or approved upstream sources. Do not commit credentials or silently add another source when restore fails.

Use the [XMediat Azure Artifacts skill](../.agents/skills/xmediat-azure-artifacts/SKILL.md) to configure authenticated access or diagnose local and CI restore failures. Select a package version confirmed to exist in the feed; a local XMediat build does not prove that version has been published.

Add the contracts package to a contract project and the runtime package to a module implementation or executable host using the approved feed version:

```bash
dotnet add path/to/Module.Contracts.csproj package XMediat.Contracts --version APPROVED_VERSION --source XelseorPackages
dotnet add path/to/Module.Module.csproj package XMediat --version APPROVED_VERSION --source XelseorPackages
```

When the repository uses central package management, declare the versions in `Directory.Packages.props` and omit project-level version attributes as required by that repository's conventions.

## Project and Module Placement

For a modular monolith:

- `[ModuleName].Contracts` references `XMediat.Contracts` and owns public messages intended to cross the module boundary.
- `[ModuleName].Module` references `XMediat` and owns handlers, behaviors, hooks, processors, and registration.
- Put each message in its matching `Requests`, `Commands`, `Queries`, `Notifications`, or `Events` folder.
- Put each handler in its matching `RequestHandlers`, `CommandHandlers`, `QueryHandlers`, `NotificationHandlers`, or `EventHandlers` folder.
- Keep implementation handlers `internal` unless a deliberate public boundary requires otherwise.
- Register the module's handler assembly from `[ModuleName]Module.cs`, the module's public composition entry point.

Messages shared outside a module must expose contracts, identifiers, and data-transfer shapes—not entities, EF Core contexts, repositories, or other implementation types.

## Message Families

Choose the family by behavior, response semantics, and handler cardinality:

| Family | Contract | Dispatch interface | Handler cardinality | Use |
|---|---|---|---|---|
| Request | `IRequest` or `IRequest<TResponse>` | `IRequestSender` | Exactly one | Neutral request/response work that is not modeled as a command or query. |
| Command | `ICommand` or `ICommand<TData>` | `ICommandDispatcher` | Exactly one | State-changing behavior with a standardized success or failure envelope. |
| Query | `IQuery<TData>` | `IQueryDispatcher` | Exactly one | Read behavior with explicit result, count, paging, and sorting metadata. |
| Notification | `INotification` | `INotificationPublisher` | Zero or more | In-process fan-out where no handler is required. |
| Event | `IEvent` or `IEvent<TData>` | `IEventPublisher` | Zero or more | An in-process fact, optionally carrying standardized event metadata and typed data. |
| Stream | `IStreamRequest<TResponse>` | `IStreamSender` | Exactly one | Cold asynchronous sequences consumed within the active dependency-injection scope. |

Use `IMediator` only when one consumer genuinely needs several of these families. Otherwise inject the narrow interface that communicates the consumer's actual capability.

Use this design checkpoint before choosing a family:

- Reads are queries.
- State changes are commands.
- Neutral request/response operations that do not need command or query envelope semantics are requests.
- Facts that have already occurred are events.
- In-process fan-out instructions or signals are notifications.

Authentication may be a neutral request when it only verifies credentials and returns session material. Account creation, password changes, password resets, role changes, and other state-changing identity workflows should normally be commands.

## Commands

Commands represent intent to change state. Expected business failures belong in `CommandResponse` or `CommandResponse<TData>` rather than exceptions. Use exceptions for unexpected failures.

Define the command in the contracts project:

```csharp
using Users.Contracts.DataTransfer;
using XMediat.Contracts;

namespace Users.Contracts.Commands;

public sealed record CreateUser(string DisplayName) : ICommand<UserDto>;
```

Implement the handler in the module project:

```csharp
using Users.Contracts.Commands;
using Users.Contracts.DataTransfer;
using XMediat;
using XMediat.Contracts;

namespace Users.Module.CommandHandlers;

internal sealed class CreateUserHandler(IUserRepository users)
    : ICommandHandler<CreateUser, UserDto>
{
    public async Task<CommandResponse<UserDto>> Handle(
        CreateUser command,
        CancellationToken cancellationToken)
    {
        UserDto user = await users.Create(command.DisplayName, cancellationToken);

        return CommandResponse<UserDto>.Success(
            data: user,
            recordsAffected: 1,
            createdIdentifiers: [user.Id.ToString()]);
    }
}
```

Return expected validation or conflict outcomes through the failure envelope:

```csharp
return CommandResponse<UserDto>.Failure(
    [new CommandError("duplicate-user", "A user with that name already exists.")]);
```

## Queries

Queries represent reads and return `QueryResponse<TData>`. The handler owns the accuracy of `Data`, `TotalCount`, `ReturnedCount`, paging, and applied-sort metadata; XMediat preserves the envelope and does not infer those values.

```csharp
using Users.Contracts.DataTransfer;
using XMediat.Contracts;

namespace Users.Contracts.Queries;

public sealed record SearchUsers(int Page, int PageSize)
    : IQuery<IReadOnlyList<UserDto>>;
```

```csharp
return new QueryResponse<IReadOnlyList<UserDto>>(
    data: users,
    totalCount: totalCount,
    returnedCount: users.Count,
    page: new QueryPage(query.Page, query.PageSize),
    appliedSorts: [new QuerySort("DisplayName", QuerySortDirection.Ascending)]);
```

## Requests

Use requests for neutral request/response behavior that does not need command or query semantics. `IRequest` produces `Unit`; `IRequest<TResponse>` produces the declared response type.

```csharp
using XMediat.Contracts;

namespace Search.Contracts.Requests;

public sealed record RebuildSearchIndex : IRequest;
```

Neutral requests may use pre-processors, post-processors, exception actions, and exception handlers. Those processor contracts do not apply to commands and queries, which retain their specialized response semantics.

## Notifications and Events

Notifications and events are separate zero-to-many families. With the default strategy, XMediat awaits handlers sequentially in deterministic registration order and stops on the first exception or cancellation.

- Use a notification for in-process fan-out where the message is primarily an instruction or signal.
- Use an event for a fact that occurred and should be expressed with event semantics.
- Do not assume publication is durable or distributed.
- Connect an event to a broker through an application-owned adapter, and use an outbox when reliable external delivery is required.
- Design handlers to remain independent; ordering should not become an undocumented business dependency.
- Prefer immutable event records and typed payloads over mutable event classes or ambiguous serialized `string` data.
- Standardize application-event metadata where auditing or integration requires it: event identifier, occurrence time, source, version, correlation identifier, and actor identifier when appropriate.
- Do not implement required event members with setters that throw. Construct a valid event atomically and keep invariant metadata read-only.
- Keep credentials, tokens, passwords, and unnecessary personal data out of events, logs, and telemetry. Use stable identifiers instead of email addresses or usernames when the consumer does not require the display value.

## Streams

Streams use cold `IAsyncEnumerable<T>`. XMediat resolves and invokes the handler when enumeration begins, not when `CreateStream` is called. Enumerate the returned stream before disposing the dependency-injection scope that owns its handler and dependencies.

## Registration

Register XMediat once at the application or module composition boundary and scan explicit handler assemblies using marker types:

```csharp
using Microsoft.Extensions.DependencyInjection;
using XMediat;

namespace Users.Module;

public static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        services.AddXMediat(configuration =>
            configuration.RegisterServicesFromAssemblyContaining<CreateUserHandler>());

        return services;
    }
}
```

If the application uses a single root XMediat registration, register each module assembly there rather than calling `AddXMediat` repeatedly. Registration must include at least one assembly. Ambiguous single handlers and invalid generic compositions fail during registration; missing single handlers fail at dispatch.

Handlers resolve from the current Microsoft dependency-injection scope. XMediat caches reflection metadata, not handler instances, service providers, messages, cancellation tokens, or results.

## Dispatch

Callers depend on the narrow dispatcher matching their operation:

```csharp
using Users.Contracts.Queries;
using Users.Contracts.DataTransfer;
using XMediat;
using XMediat.Contracts;

namespace Users.WebApi;

internal sealed class UserEndpoint(IQueryDispatcher queries)
{
    public async Task<IReadOnlyList<UserDto>> Handle(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        QueryResponse<IReadOnlyList<UserDto>> response = await queries.Query(
            new SearchUsers(page, pageSize),
            cancellationToken);

        return response.Data;
    }
}
```

Do not resolve dispatchers through `IServiceProvider` inside handlers. Inject collaborators normally and dispatch within the active scope.

## Cross-Cutting Behavior

Use the lightest mechanism that matches the requirement:

- Continuation pipeline behaviors can run code around a request, command, query, or stream handler, short-circuit, replace a result, or perform deliberate sequential retries.
- Lifecycle hooks observe before, succeeded, expected-failed, exception, and finally stages without controlling the handler result.
- Request processors provide pre-processing, post-processing, exception actions, and exception recovery for neutral requests only.
- Notifications and events do not use continuation pipelines.

Register cross-cutting behavior against narrow message families or application-specific marker interfaces. Keep ordering deliberate and test it when outcome depends on the sequence. Enable automatic open-generic handler closure only when required, and retain explicit bounds for type parameters, closing types, registrations, and registration time.

## Validation Ownership

- Choose one authoritative execution point for each validation concern.
- Prefer an XMediat pipeline behavior for application-message validation that must apply regardless of caller.
- Use HTTP-boundary validation only for transport-specific concerns such as binding shape, headers, route values, or wire-format requirements.
- Do not manually instantiate validators inside handlers; inject a collaborator or rely on the registered pipeline so validation remains composable and testable.
- Keep ownership-neutral reusable rule extensions in the shared Core contracts project.
- Keep module-specific validator types with the module contracts when every caller must share them, or in the module implementation when they are internal application rules.
- Do not execute the same validator independently at both the transport boundary and inside the handler unless the duplication is deliberate and documented.

## Cancellation and Failure Semantics

- Pass the caller's cancellation token into the dispatcher and every downstream asynchronous operation.
- Allow unexpected handler exceptions to propagate unless a documented request exception handler owns recovery.
- Return expected command failures through command response envelopes.
- Keep query metadata truthful even for empty or partial results.
- Expect default notification and event publication to stop on the first exception or cancellation.
- Do not wrap every exception merely to hide its source; translate failures only at an appropriate application or transport boundary.

## Testing

- Unit test each handler as one use case or business outcome with its dependencies substituted.
- Test successful and expected-failure command envelopes separately.
- Verify query data and every applicable count, page, and sort field.
- Integration test XMediat registration and assembly scanning for each module.
- Add an architecture test proving contracts projects do not reference module implementations or the XMediat runtime package.
- Add an architecture test proving message-declaring projects directly reference `XMediat.Contracts` and handler or dispatcher projects directly reference `XMediat`.
- Test application validation through its authoritative pipeline and transport-specific validation at the HTTP boundary.
- Test pipeline, hook, processor, and publication ordering only where the ordering is intentional behavior.
- Verify cancellation reaches the handler and critical dependencies.
- For streams, test enumeration and disposal inside an active scope.

## Runtime Boundary

XMediat uses runtime reflection and supports .NET 10 JIT execution with default, non-collectible assemblies. NativeAOT, arbitrary trimming, dynamic scanned assemblies, and collectible or unloadable plugin assembly contexts are unsupported. Do not suppress the library's `RequiresDynamicCode` or `RequiresUnreferencedCode` diagnostics without resolving the underlying deployment mismatch.

## Avoid

- Putting handlers or runtime dependencies in a contracts project.
- Relying on transitive project references for a project's XMediat package dependency.
- Manually constructing validators inside handlers or accidentally validating the same rule at multiple layers.
- Sending commands from code that should call a cohesive domain collaborator directly.
- Using `IMediator` everywhere as a service locator with a friendlier name.
- Modeling a state change as a query or an ordinary expected business failure as an exception.
- Treating notifications or events as durable external delivery.
- Registering every loaded assembly when explicit module assemblies are known.
- Enabling unbounded open-generic discovery.
- Dispatching streams after their dependency-injection scope has been disposed.
- Assuming MediatR APIs or behavior exist in XMediat.
- Committing Azure Artifacts credentials or inferring feed publication from local packages.

## Source of Truth

This guide is derived from the current XMediat source and documentation:

- `/Users/john/Source/repos/XMediat/README.md`
- `/Users/john/Source/repos/XMediat/XMediat/XMediat.Contracts`
- `/Users/john/Source/repos/XMediat/XMediat/XMediat`
- `/Users/john/Source/repos/XMediat/docs/MIGRATION.md`
- `/Users/john/Source/repos/XMediat/docs/RELEASING.md`

Reinspect those sources before changing guidance about supported APIs, versions, licensing, package availability, or runtime constraints.
