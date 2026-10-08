# .NET Aspire 13.3+

## Purpose

Define the default architecture guidance for building and operating distributed .NET applications with Aspire 13.3+.

## When To Use

- The system contains multiple services, frontends, workers, containers, or infrastructure resources that should be modeled together.
- The team wants local orchestration, service discovery, and observability to be part of the application architecture instead of scattered environment setup.
- The application should stay deployable across local development, containers, Kubernetes, Azure, or other hosting targets without rewriting the system model each time.

## Version Baseline

- Target Aspire **13.3 or later** for new Aspire work unless the repository is pinned lower.
- Always create both the **AppHost** project and the **ServiceDefaults** project using Aspire **13.3** conventions and package versions.
- Treat the AppHost as the source of truth for resource topology, wiring, and local orchestration.
- Use the current Aspire CLI and update Aspire packages together so the AppHost SDK, integrations, and tooling stay aligned.
- Aspire 13 requires **.NET 10 SDK or later**.

## Core Principles

- Model the system in code, not in scattered local scripts and ad hoc configuration files.
- Keep Aspire responsible for orchestration, resource wiring, discovery, and developer observability.
- Keep business behavior inside the application projects, not inside the AppHost.
- Make service relationships explicit through references instead of hidden environment assumptions.
- Design deployable services to remain independently understandable even when Aspire coordinates them together.

## Recommended Structure

- One AppHost project owns the distributed application topology.
- One shared Service Defaults project configures cross-cutting concerns such as OpenTelemetry, resilience, and common HTTP behavior.
- One shared class library named `<RootNamespace>.AspireConstants` owns shared Aspire resource-name constants.
- Each API, web app, worker, function-style process, or executable remains its own project with clear ownership.
- Infrastructure resources such as databases, caches, queues, storage, and external integrations should be declared in the AppHost and referenced explicitly by dependent services.
- Keep deployment-specific packaging separate from domain logic so the application can evolve without coupling core behavior to one hosting target.

## Shared Constants Project

- Do not use raw string literals for Aspire resource names in the AppHost.
- Create a class library named `<RootNamespace>.AspireConstants`.
- In the `.csproj`, set `IsAspireSharedProject` to `true`.
- Reference the `AspireConstants` project from the AppHost project.
- On that AppHost project reference, set `IsAspireProjectResource="false"` so the constants project is treated as shared code, not as an Aspire resource.
- Use the constants from this project both in `Aspire.AppHost` and in application projects that need to refer to the same logical Aspire resources.

## AppHost Boundaries

- The AppHost should declare resources, references, endpoints, and startup composition.
- Do not move domain workflows, business rules, or request orchestration into the AppHost.
- Keep AppHost code readable enough that a teammate can understand the system topology from one file or one small set of files.
- Prefer explicit resource names that match the business or deployable concern they represent.
- Prefer constants from `<RootNamespace>.AspireConstants` over inline names for projects, modules, shared databases, caches, and other declared resources.

## Resource Constants Layout

- Keep a single static class hierarchy rooted at `Resources`.
- Group constants by shared infrastructure, modules, and projects so naming stays discoverable and consistent.
- Use nested static classes to reflect the logical system shape instead of a flat file full of unrelated names.

Example shape:

```csharp
public static class Resources
{
    public static class Base
    {
        public const string SqlServer = "sql";
        public const string SharedDatabase = "shared-db";
    }

    public static class Modules
    {
        public static class Billing
        {
            public const string Database = "billing-db";
            public const string Cache = "billing-cache";
        }
    }

    public static class Projects
    {
        public static class BillingApi
        {
            public const string Name = "billing-api";
        }
    }
}
```

Suggested hierarchy:

- `Resources`
- `Resources.Base` for shared infrastructure such as SQL Server, shared databases, caches, queues, or other cross-module resources
- `Resources.Modules.<ModuleName>` for module-specific resources
- `Resources.Projects.<ProjectName>` for project-specific constants such as the Aspire project name

## MSBuild Metadata

`<RootNamespace>.AspireConstants.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsAspireSharedProject>true</IsAspireSharedProject>
  </PropertyGroup>
</Project>
```

`<RootNamespace>.AppHost.csproj` project reference:

```xml
<ItemGroup>
  <ProjectReference Include="..\\<RootNamespace>.AspireConstants\\<RootNamespace>.AspireConstants.csproj"
                    IsAspireProjectResource="false" />
</ItemGroup>
```

## Service Discovery And Configuration

- Use `WithReference()` to declare service dependencies instead of hardcoding URLs or ports in application code.
- Let services talk to logical names and let Aspire resolve the runtime address.
- Use named endpoints only when a resource truly exposes multiple distinct surfaces such as a public API and an admin or dashboard endpoint.
- Do not leak local-only port assumptions into project configuration, tests, or frontend code.
- The logical names used in `AddProject`, `AddContainer`, `AddSqlServer`, `AddDatabase`, and similar AppHost declarations should come from `Resources` constants rather than inline strings.

## Observability And Operations

- Treat telemetry as part of the default architecture, not a later enhancement.
- Keep OpenTelemetry configured through the shared service defaults project so logging, tracing, and metrics stay consistent across services.
- Use the Aspire dashboard during development to inspect health, logs, traces, metrics, and dependency wiring.
- Prefer health checks, structured logging, and traceable outbound calls in every long-running service.

## Data And Integration Patterns

- Model databases, caches, messaging systems, and external dependencies as explicit resources where Aspire support exists.
- Keep data ownership with the service that owns the behavior, even when multiple resources are orchestrated together.
- Prefer asynchronous workflows for cross-service work that does not require an immediate synchronous response.
- Keep integration contracts explicit and versionable when services may evolve independently.
- Use external parameters and environment-specific configuration only for values that legitimately vary by environment.

## Deployment Guidance

- Treat Aspire as the application topology model across local development and deployment packaging.
- Keep deployment concerns aligned with supported Aspire workflows instead of creating a second competing orchestration model by default.
- Be deliberate when introducing Kubernetes, Azure, or container-specific features so the AppHost remains understandable to developers working locally.
- Document any environment-specific publishing or deployment conventions next to the project’s operational guidance, not hidden in tribal knowledge.

## Testing

- Unit test service logic inside the owning application projects.
- Integration test resource boundaries and service-to-service workflows where contracts matter.
- Use Aspire testing support for distributed application tests that need the AppHost and resource graph running together.
- Add end-to-end workflow tests for critical multi-service user journeys.

## Avoid

- Treating the AppHost as the place for business logic.
- Hardcoded service URLs, ports, or credentials inside application code.
- Hidden dependencies between services that are not declared through references.
- Letting deployment-specific hacks replace the AppHost as the system source of truth.
- Pulling every possible resource into one giant AppHost when separate bounded systems would be clearer.
