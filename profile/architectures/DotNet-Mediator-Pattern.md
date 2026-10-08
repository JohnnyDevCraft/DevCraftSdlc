# .NET Mediator Pattern

## Purpose

Define how .NET applications use the mediator pattern through the `Mediator` library. The mediator is the in-process boundary between callers and application behavior: callers express intent as messages, while Mediator locates and invokes matching handlers.

## When To Use

- Use the mediator pattern to keep endpoints, user interfaces, hosted services, and other callers independent of concrete application handlers.
- Use it when commands, queries, notifications, or requests benefit from consistent dispatch, cancellation, validation, or pipeline behavior.
- Use it to make module entry points explicit in a modular monolith.
- Do not use it to hide arbitrary service calls or replace clear domain collaboration inside a cohesive component.
- Do not treat in-process publication as durable messaging. External brokers, retries, delivery guarantees, and outbox behavior remain separate integration concerns.

## Core Principles

- Messages describe intent or facts; handlers perform application behavior.
- Define messages in application or contract projects and handlers in implementation projects.
- Prefer one handler responsibility per type and one hand-authored type per file.
- Select command, query, request, or notification semantics based on the operation.
- Inject the narrow sender, publisher, or mediator abstraction the caller actually needs.
- Keep handlers thin enough to coordinate domain services, repositories, and integrations without becoming unstructured transaction scripts.
- Propagate the caller's `CancellationToken` through dispatch and every asynchronous dependency.
- Keep transport concerns such as HTTP status codes, controllers, endpoints, and UI models outside handlers and shared message contracts.

## Mediator Library

Use the `Mediator` NuGet package as the default mediator library for this architecture.

The package is designed around compile-time source generation. Favor the library's actual message, handler, sender, publisher, pipeline, and notification contracts instead of assuming MediatR or XMediat conventions apply.

Reference the package where messages are dispatched or handlers are implemented. In larger solutions, keep contracts and implementation placement intentional:

- Contract or application-abstraction projects own shared messages.
- Module or application implementation projects own handlers and registration.
- Executable hosts own dependency injection composition.

## Project and Module Placement

For a modular monolith:

- `[ModuleName].Contracts` or `[ModuleName].Application` owns public messages intended to cross a module boundary.
- `[ModuleName].Module` owns handlers, behaviors, pipelines, and registration.
- Put each message in a folder that matches its role, such as `Commands`, `Queries`, `Requests`, `Notifications`, or `Events`.
- Put each handler in a matching handler folder, such as `CommandHandlers`, `QueryHandlers`, `RequestHandlers`, or `NotificationHandlers`.
- Keep implementation handlers `internal` unless a deliberate public boundary requires otherwise.
- Register handlers and Mediator services from the module or application composition boundary.

Messages shared outside a module must expose contracts, identifiers, and data-transfer shapes, not entities, EF Core contexts, repositories, or other implementation types.

## Message Families

Choose message shape by behavior and response semantics:

- Commands change state.
- Queries read state.
- Requests represent neutral request/response work that is not clearly a command or query.
- Notifications represent in-process fan-out.

Use a broad mediator abstraction only when one consumer genuinely needs several message families. Otherwise inject the narrow sender or publisher abstraction that communicates the consumer's actual capability.

## Commands

Commands represent intent to change state. Expected business failures should be modeled explicitly in the application response shape rather than hidden as infrastructure exceptions. Use exceptions for unexpected failures.

Keep command contracts small and focused. A command should describe the requested state change, not expose persistence or transport concerns.

## Queries

Queries represent reads. Query handlers should not mutate state. The handler owns the accuracy of returned data, counts, paging, and sorting metadata when those concepts apply.

Prefer application-specific read models or DTOs over returning persistence entities directly.

## Requests

Use requests for neutral request/response behavior that does not need command or query semantics.

Examples include:

- Rebuilding a local cache.
- Performing an internal calculation.
- Coordinating a non-state-changing application service operation.

## Notifications

Notifications are in-process fan-out and may have zero or more handlers.

- Use notifications for in-process signals.
- Keep handlers independent.
- Do not assume notification publication is durable or distributed.
- Connect durable integration events to an external broker through an application-owned adapter and use an outbox when reliable external delivery is required.

## Pipelines and Behaviors

Use pipeline behaviors for cross-cutting concerns that truly apply around message handling:

- Validation
- Logging
- Telemetry
- Authorization checks
- Transaction boundaries

Do not put business logic in generic pipeline code when it belongs in a handler or domain service.

## Registration

Register Mediator once at the application or module composition boundary. Keep assembly registration explicit so handler discovery remains predictable.

If a repository already contains established registration conventions, follow those conventions instead of introducing a second composition style.

## Testing

- Unit test handlers through their public handler method or through the mediator when pipeline behavior is part of the test subject.
- Test domain logic outside handlers when handlers only coordinate domain services.
- Add integration tests around registration so missing handlers or misconfigured pipelines fail early.
- Keep tests deterministic and avoid relying on handler discovery order unless the library explicitly guarantees it.

## Avoid

- Treating the mediator as a service locator.
- Dispatching messages from deep inside domain entities.
- Chaining mediator calls across handlers without a clear application boundary.
- Hiding simple direct collaboration behind mediator messages when direct dependency injection is clearer.
- Assuming MediatR or XMediat APIs, behaviors, or envelopes apply to the `Mediator` library.

## Notes

- When a repository already has a mediator library and conventions, prefer the existing local conventions unless the operator explicitly chooses to migrate.
- Keep this architecture focused on the `Mediator` library. Use the XMediat-specific architecture when a project intentionally uses XMediat.

