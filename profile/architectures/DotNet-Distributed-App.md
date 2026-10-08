# .NET Distributed App

## Purpose

Define the default architecture guidance for a .NET distributed application made up of multiple deployable parts.

## When To Use

- Different workloads need independent scaling or deployment.
- Separate applications or services collaborate over the network.
- Infrastructure concerns justify multiple deployable units.

## Core Principles

- Treat network boundaries as first-class design constraints.
- Make service contracts explicit and versionable.
- Design for observability, resiliency, and operational clarity.
- Keep each deployable unit independently understandable.

## Recommended Structure

- Separate solutions or top-level apps for each deployable service, worker, gateway, or frontend.
- Shared contracts or SDKs only when they reduce duplication without overcoupling teams.
- Centralized configuration, logging, tracing, and deployment conventions.

## Service Boundaries

- Each service owns its own behavior and data.
- Do not treat another service's database as a shared dependency.
- Keep APIs intentional and consumer-focused.
- Favor backward-compatible contract changes.

## Data and Integration

- Use HTTP, messaging, or event-driven integration intentionally based on workflow needs.
- Plan for retries, timeouts, and partial failure.
- Prefer idempotent handlers for externally triggered work.

## Testing

- Unit test service logic.
- Integration test infrastructure boundaries.
- Add contract tests where services integrate across teams or repos.
- Exercise end-to-end workflows for critical paths.

## Avoid

- Hidden cross-service coupling through shared databases.
- Chatty synchronous calls for workflows that should be asynchronous.
- Shared libraries that pull many services into lockstep release cycles.
