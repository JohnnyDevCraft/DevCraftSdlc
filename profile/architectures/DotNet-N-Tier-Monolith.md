# .NET N-Tier Monolith

## Purpose

Define the default architecture guidance for a .NET n-tier monolith.

## When To Use

- One deployable application is desired.
- The team prefers clear separation between presentation, application, domain, and data access layers.
- Module-level separation is less important than layer-level consistency.

## Core Principles

- Keep dependencies flowing inward toward the core business logic.
- Separate responsibilities by layer clearly.
- Preserve one deployable unit while avoiding mixed concerns.

## Recommended Structure

- Presentation layer for UI or API concerns.
- Application layer for orchestration and use cases.
- Domain layer for business rules and core models.
- Infrastructure or data layer for persistence and external integrations.

## Layer Boundaries

- Presentation should not contain business logic.
- Domain should not depend on infrastructure.
- Data access should not leak directly into UI concerns.
- Application services coordinate work across the domain and infrastructure.

## Data and Integration

- Keep repositories or persistence adapters in infrastructure.
- Define clear DTOs, requests, or view models between layers where needed.
- Avoid exposing EF entities directly across every layer by default.

## Testing

- Unit test domain and application layers heavily.
- Integration test persistence and HTTP behavior.
- Use architecture tests where practical to enforce layer dependencies.

## Avoid

- Putting all business rules in controllers or repositories.
- Creating pass-through layers that add no value.
- Allowing every layer to reference every other layer.
