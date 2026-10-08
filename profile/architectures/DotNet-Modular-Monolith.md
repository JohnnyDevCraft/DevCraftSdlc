# .NET Modular Monolith

## Purpose

Define the default architecture guidance for a .NET modular monolith application.

## When To Use

- One deployable unit is preferred.
- The domain is large enough to require clear module boundaries.
- Teams want strong internal separation without distributed system overhead.
- The product may later split selective modules into services.

## Core Principles

- Ship as one application, design as many modules.
- Keep modules isolated by contracts, not by folder names alone.
- Prefer synchronous in-process collaboration until there is a clear reason to distribute.
- Make module ownership and boundaries obvious in code.

## Recommended Structure

- One solution with clearly separated module projects or module folders.
- Shared host or web application as the composition root.
- Each module owns its application logic, domain logic, persistence, and integration adapters where practical.
- Cross-module dependencies should flow through explicit interfaces, contracts, or events.

### Folder Structure

Use `src` as the source root and organize projects by hosting concern, bounded module, web application, and mobile application:

```text
src/
├── Aspire/
│   ├── AppHost/
│   ├── AspireConstants/
│   └── ServiceDefaults/
├── Modules/
│   ├── Core/
│   │   ├── [AppName].Core.Contracts/
│   │   └── [AppName].Core.Module/
│   └── [ModuleName]/
│       ├── [ModuleName].Contracts/
│       │   ├── Enums/
│       │   ├── Requests/
│       │   ├── Events/
│       │   ├── Notifications/
│       │   ├── Constants/
│       │   ├── Configuration/
│       │   ├── DataTransfer/
│       │   ├── Exceptions/
│       │   └── Interfaces/
│       └── [ModuleName].Module/
│           ├── DataAccess/
│           │   ├── Entities/
│           │   ├── DataContext/
│           │   └── Migrations/
│           ├── Mappers/
│           ├── RequestHandlers/
│           ├── CommandHandlers/
│           ├── QueryHandlers/
│           ├── Commands/
│           ├── Queries/
│           ├── Validators/
│           ├── NotificationHandlers/
│           ├── EventHandlers/
│           ├── Services/
│           ├── InternalInterfaces/
│           └── [ModuleName]Module.cs
├── Web/
│   ├── BlazorSSR/
│   ├── BlazorWasm/
│   ├── WebApi/
│   ├── SharedAPI/
│   └── SharedBlazor/
└── Mobile/
    ├── BlazorHybrid/
    ├── Android/
    ├── IOS/
    ├── Watch/
    └── Other/
```

The folders have these responsibilities:

- `Aspire/AppHost` is the Aspire orchestration and composition host.
- `Aspire/AspireConstants` contains constant values used by the AppHost and referenced by application projects.
- `Aspire/ServiceDefaults` contains shared service defaults and cross-cutting host configuration.
- `Modules/Core/[AppName].Core.Contracts` is the ownership-neutral shared contract assembly for primitives, common event metadata, reusable validation rules, and cross-module abstractions.
- `Modules/Core/[AppName].Core.Module` is the one intentional shared implementation module for reusable cross-cutting infrastructure. It must not own a business module's entities, persistence, or workflows.
- `Modules/[ModuleName]/[ModuleName].Contracts` contains the module's public integration surface. Organize its types into `Enums`, `Requests`, `Events`, `Notifications`, `Constants`, `Configuration`, `DataTransfer`, `Exceptions`, and `Interfaces` as applicable.
- `Modules/[ModuleName]/[ModuleName].Module` contains the module's internal implementation. Its `DataAccess` folder owns entities, the data context, and migrations.
- `Mappers` contains static classes with explicit mapper methods. Do not use AutoMapper.
- `[ModuleName]Module.cs` is the module's public composition entry point and wires the module into dependency injection.
- `Web/BlazorSSR` is used for public websites with public frontend applications.
- `Web/BlazorWasm` is used when the application requires a progressive web app (PWA).
- `Web/WebApi` is used for a PWA or when the system includes mobile applications.
- `Web/SharedAPI` contains endpoints shared by `BlazorSSR` and `WebApi`.
- `Web/SharedBlazor` contains Blazor components shared by `BlazorSSR`, `BlazorWasm`, and `BlazorHybrid`.
- `Mobile/BlazorHybrid`, `Mobile/Android`, `Mobile/IOS`, and `Mobile/Watch` contain their respective mobile clients. Add other platform-specific projects under `Mobile/Other` when required.

Only create the web and mobile projects required by the product. Folder placement communicates architectural ownership; project references must still enforce the module boundaries defined below.

### Project Reference Direction

Keep the dependency graph explicit and enforce it with architecture tests:

```text
Host -> Module -> Module.Contracts -> Core.Contracts
          |
          +----> Core.Module -> Core.Contracts
```

- A contracts project must never reference its implementation module.
- A business module must not reference another business module's implementation assembly.
- Core contracts and the shared Core module must not reference a business module.
- HTTP, UI, EF Core, and hosting types must not leak into module contracts.
- A project that declares XMediat messages must reference `XMediat.Contracts` directly; do not hide that architectural dependency behind a transitive project reference.
- A project that implements XMediat handlers or dispatches messages must reference `XMediat` directly; do not rely on another module to supply the runtime package transitively.

### HTTP Host Module Adapters

Mirror bounded modules inside each HTTP host so transport ownership is easy to trace:

```text
WebApi/
└── Modules/
    └── [ModuleName]/
        ├── [ModuleName]Group.cs
        └── [UseCase]Endpoint.cs
```

- Host module folders contain transport adapters, not module implementation logic.
- Endpoints depend on public module contracts and the narrow XMediat dispatcher or publisher they require.
- Keep endpoints thin: bind and validate transport input, dispatch one application message, and translate the result into HTTP semantics.
- Keep handlers free of FastEndpoints, controllers, HTTP results, status codes, and other transport concerns.

### Mediator Pattern with XMediat

Use Xelseor LLC's `XMediat` library as the default mediator implementation for .NET modular monolith applications. Consume it from the private `XelseorPackages` Azure Artifacts feed rather than copying XMediat source into an application repository.

Place this `NuGet.config` beside the solution or consuming project:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="XelseorPackages" value="https://pkgs.dev.azure.com/xelseor/_packaging/XelseorPackages/nuget/v3/index.json" />
  </packageSources>
</configuration>
```

The `<clear />` element intentionally removes inherited package sources. The Azure Artifacts feed must therefore supply every dependency required by the application, either directly or through approved upstream sources. Do not silently add another package source when restore fails.

Apply the packages at module boundaries as follows:

- Reference `XMediat.Contracts` from `[ModuleName].Contracts` projects that define shared requests, commands, queries, notifications, events, and response contracts.
- Reference `XMediat` from `[ModuleName].Module` projects that implement handlers or dispatch messages. The runtime package supplies the matching contracts dependency transitively.
- Keep handlers in their matching `RequestHandlers`, `CommandHandlers`, `QueryHandlers`, `NotificationHandlers`, or `EventHandlers` folders.
- Register XMediat and its handler assembly from `[ModuleName]Module.cs` as part of the module's dependency-injection wiring.
- Prefer the narrow dispatcher or publisher interface required by a consumer. Use `IMediator` only when one consumer genuinely needs several message families.
- Keep XMediat contracts free of ASP.NET Core, UI, persistence, and module-implementation dependencies.
- Select a package version that is confirmed to exist in `XelseorPackages`; do not infer Azure Artifacts availability from a local XMediat build.

Apply the [.NET Mediator Pattern with XMediat](./DotNet-Mediator-Pattern.md) guidance for message selection, handler design, registration, dispatch, cross-cutting behavior, failure semantics, and testing. For authenticated local setup and restore troubleshooting, use the [XMediat Azure Artifacts skill](../.agents/skills/xmediat-azure-artifacts/SKILL.md).

## Module Boundaries

- A module should expose a narrow public surface.
- Internal implementation details should stay hidden.
- In a module implementation assembly, `public` should be reserved for the module bootstrap/composition entry point only, such as `BillingModule`.
- Entities, EF Core contexts, repositories, repository abstractions, services, mappers, options, workers, and other implementation types inside a module implementation assembly should default to `internal`.
- If tests or tightly-scoped internal infrastructure need access to module internals, use `InternalsVisibleTo` intentionally instead of widening the module surface to `public`.
- Treat a `public` implementation type inside a module assembly as an architecture bug, not a style preference.
- Exception: a modular monolith may define one intentional shared module (for example `*.Shared.Module`) whose purpose is reuse across modules.
- In that one shared module, using `public` reusable types is acceptable and expected; that assembly is designed to be consumed by other modules.
- The shared module must not become a back door into another module's persistence or business ownership; it should host only truly shared contracts, primitives, helpers, and cross-cutting infrastructure.
- Avoid direct database access across modules.
- Avoid referencing another module's internals just because the code is in the same process.
- Cross-module references should cross boundaries by record id, not by direct entity coupling.
- If a module needs to render or retain another module's display text, store a local snapshot such as `RemoteId` and `RemoteLabel` instead of reaching through the boundary at read time.

## Module Composition and Lifecycle

- Give each module one public composition facade, such as `AddAuthModule(...)`; keep its implementation collaborators internal.
- A module owns its database migration and optional seed lifecycle. The host may invoke a public module lifecycle hook, but it must not resolve or manipulate the module's internal `DbContext` directly.
- Separate registration, migration, and seeding implementation into focused types when the public facade begins accumulating responsibilities.
- Keep seed behavior explicitly environment-gated. Never commit production credentials or reusable default passwords in seed code.
- Check and surface migration, role-creation, user-creation, and other framework operation results; do not silently continue after a failed lifecycle step.
- Accept and propagate `CancellationToken` through asynchronous lifecycle hooks.

## Data and Integration

- Prefer per-module persistence boundaries even if the same database server is used.
- Use application services, contracts, or internal messaging for cross-module workflows.
- Publish domain or integration events when loose coupling improves clarity.
- When a source module changes data that other modules mirror or display, publish an event with the updated state so dependent modules can refresh cached labels or read models.
- Treat event publication as the default rule for successful data-changing commands that other modules may care about, not as an optional afterthought.
- If the solution uses repositories, application services inside the module must talk to persistence through repository abstractions, not directly through `DbContext`.
- EF Core `DbContext` access belongs inside repository implementations and tightly-scoped persistence infrastructure, not in the service layer.
- Service-layer calls to `DbSet<>`, `SaveChanges`, or `SaveChangesAsync` are boundary violations in a repository-based module architecture.
- Put each hand-authored `IEntityTypeConfiguration<TEntity>` in its own file, normally under `DataAccess/Configurations`.
- Every module `DbContext` must deliberately apply its entity configurations, such as through `ApplyConfigurationsFromAssembly`, rather than relying on configuration types merely existing in the assembly.
- Add an integration test that proves important entity constraints and configurations are present in the constructed EF Core model.

## Aspire Resource Ownership

- Keep shared infrastructure names under `Resources.Base` or `Resources.Dependencies`.
- Keep module-owned resources under `Resources.Modules.<ModuleName>` and deployable project names under `Resources.Projects`.
- Use `Database` as the standard identifier spelling.
- Give each module its own logical database resource name even when several module databases share one SQL Server resource.
- Keep resource names environment-neutral; supply environment-specific values through Aspire parameters and configuration.

## Testing

- Unit test module logic in isolation.
- Integration test module boundaries and host wiring.
- Add architecture tests that enforce project-reference direction, contracts purity, module implementation visibility, and the absence of cross-module implementation references.
- Add an architecture test proving each message-declaring contracts project directly references `XMediat.Contracts` and does not reference `XMediat`.
- Add boundary-focused tests for id-only references, event publication, and remote-label refresh behavior when modules mirror cross-module display data.

## Avoid

- Shared utility layers that become a back door around module boundaries.
- Cross-module table access without an explicit contract.
- A single application project that mixes every feature together.
