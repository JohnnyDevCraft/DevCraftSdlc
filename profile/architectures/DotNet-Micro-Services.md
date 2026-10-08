# .NET Micro Services

## Purpose

Define the default architecture guidance for a .NET microservices system.

## When To Use

- The domain can be split into independently deployable bounded contexts.
- Teams need high autonomy across services.
- Independent scaling, release cadence, and ownership are required.
- The organization is prepared for distributed system complexity.

## Core Principles

- Service boundaries follow business capabilities, not technical layers.
- Each microservice owns its own data and lifecycle.
- Operational concerns are part of the design, not an afterthought.
- Prefer autonomy over convenience when choosing boundaries.

## Recommended Structure

- One service per bounded context or narrowly scoped business capability.
- Separate deployable apps for APIs, workers, and event processors as needed.
- Shared platform components for observability, security, and delivery pipelines.

## Service Design

- Keep services small enough to own well, but large enough to be meaningful.
- Expose explicit API and event contracts.
- Avoid shared domain models across services.
- Keep synchronous dependencies few and intentional.
- Cross-service references should cross boundaries by record id, not by shared entity shape or direct persistence assumptions.
- If a service must retain another service's display text for local reads, persist a local snapshot such as `RemoteId` and `RemoteLabel` and refresh it through events.

## Data and Integration

- Use database-per-service.
- Prefer event-driven communication for cross-service propagation where eventual consistency is acceptable.
- Handle duplicates, retries, and out-of-order events safely.
- Model sagas or process managers when workflows cross service boundaries.
- When service data changes in a way that downstream services mirror or display, emit an updated-state event so dependent services can refresh local labels, projections, and cached read data.
- Treat successful data-changing commands as event sources by default whenever other services rely on the changed state.

## Testing

- Unit test domain and application logic.
- Contract test APIs and event schemas.
- Integration test infrastructure and messaging flows.
- Run end-to-end tests only for a focused set of critical business flows.
- Add tests for duplicate handling, out-of-order delivery tolerance, and local snapshot refresh when cross-service labels are mirrored.

## Avoid

- Splitting into microservices before stable domain boundaries exist.
- Shared persistence across services.
- Excessive platform abstraction that hides what each service actually does.
