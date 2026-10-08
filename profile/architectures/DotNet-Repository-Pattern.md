# .NET Repository Pattern

## Purpose

Define a reusable repository pattern for C# and .NET applications that use Entity Framework Core while preserving clear module boundaries, readable query composition, and testable data access.

## When To Use

- The application uses EF Core and wants shared repository abstractions.
- Multiple modules or services need a consistent persistence pattern.
- The team wants repositories to stay small while still allowing expressive query composition.
- Shared infrastructure should be reused across modular monoliths or microservices.

## Core Principles

- Keep the generic repository surface small and stable.
- Let EF Core and LINQ do the heavy lifting instead of wrapping every query concern in custom repository methods.
- Put domain-specific filters and include graphs close to the aggregate that owns them.
- Prefer composable `IQueryable<TEntity>` extensions or query specifications over large "god repository" classes.
- Do not hide EF Core so aggressively that normal includes, ordering, paging, and projections become awkward.
- If the application adopts repositories, services must use those repositories instead of bypassing them with direct `DbContext` access.

## Shared Library Placement

For modular monoliths or microservices, place these shared types in a shared library:

- `<AppName>.Shared.Module`

Recommended structure:

```text
<AppName>.Shared.Module
/Abstractions
  IQueryRepository.cs
  ICommandRepository.cs
/Repositories
  BaseEfQueryRepository.cs
  BaseEfCommandRepository.cs
```

When a concrete module owns an entity repository, also create a module-local abstraction for that entity:

```text
<AppName>.<ModuleName>
/Abstractions
  IEntityRepository.cs
/Repositories
  EntityRepository.cs
```

The shared library owns the generic contracts. The module owns the entity-specific repository contract.

## Service Layer Boundary

In a repository-based application:

- services should depend on repository abstractions, not EF Core `DbContext` implementations
- services should not access `DbSet<>` directly
- services should not call `SaveChanges` or `SaveChangesAsync` directly on a `DbContext`
- repository implementations should own EF Core persistence details
- if a save boundary or unit-of-work abstraction is needed, define it as part of the repository infrastructure instead of injecting `DbContext` into services

Treat direct `DbContext` access from a service as an architecture violation, not as a harmless shortcut.

## Recommended Base Abstractions

The generic repository should expose a query root and a small command surface.

```csharp
public interface IQueryRepository<TEntity, TKey>
    where TEntity : class
{
    IQueryable<TEntity> Query();
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
}

public interface ICommandRepository<TEntity, TKey> : IQueryRepository<TEntity, TKey>
    where TEntity : class
{
    ValueTask<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
```

## Module-Owned Entity Repository Interfaces

When building repositories inside a module, define a matching entity-specific interface in that module's `Abstractions` folder.

Recommended example:

```csharp
public interface IPersonRepository : ICommandRepository<Person, Guid>
{
    Task<Person?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    IQueryable<Person> WithJobsAndDepartments();
    IQueryable<Person> InDepartment(string departmentName);
    IQueryable<Person> MinAge(int minAge);
}
```

Use this interface to expose:

- entity-specific lookup methods that are part of the repository contract
- reusable query entry points that are important to consumers of the module

Do not add generic cross-cutting helpers such as paging or tracking toggles to every entity interface. Those belong in shared query extensions or specifications.

## Recommended EF Base Repositories

Keep the EF base classes generic and infrastructure-focused.

```csharp
public abstract class BaseEfQueryRepository<TEntity, TKey> : IQueryRepository<TEntity, TKey>
    where TEntity : class
{
    protected BaseEfQueryRepository(DbContext dbContext)
    {
        DbContext = dbContext;
        Set = dbContext.Set<TEntity>();
    }

    protected DbContext DbContext { get; }
    protected DbSet<TEntity> Set { get; }

    public virtual IQueryable<TEntity> Query()
    {
        return Set.AsQueryable();
    }

    public abstract Task<TEntity?> GetByIdAsync(
        TKey id,
        CancellationToken cancellationToken = default);
}

public abstract class BaseEfCommandRepository<TEntity, TKey>
    : BaseEfQueryRepository<TEntity, TKey>, ICommandRepository<TEntity, TKey>
    where TEntity : class
{
    protected BaseEfCommandRepository(DbContext dbContext) : base(dbContext)
    {
    }

    public virtual async ValueTask<TEntity> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        await Set.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual void Update(TEntity entity)
    {
        Set.Update(entity);
    }

    public virtual void Remove(TEntity entity)
    {
        Set.Remove(entity);
    }
}
```

## Query Structure Recommendation

The repository itself should not become the fluent DSL.

Instead:

1. `Query()` returns `IQueryable<TEntity>`.
2. Generic cross-cutting query helpers live in shared extensions.
3. Aggregate-specific filters live in aggregate-specific extension methods.
4. Reusable complex query bundles can be expressed as specifications.

This preserves a fluent style while staying aligned with EF Core.

For most entity-specific filters, prefer one of these two patterns:

1. expose them as `IQueryable<TEntity>` extension methods owned by the module
2. expose a small number of intentional repository methods when the filter is part of the module contract and should be discoverable through the interface

## Preferred Fluent Style

Small adjustment from the original expectation:

```csharp
var people = await personRepository
    .Query()
    .WithJobsAndDepartments()
    .InDepartment("Sales")
    .MinAge(18)
    .OrderByLastThenFirst()
    .Page(page: 3, size: 20)
    .ToListAsync(cancellationToken);
```

This is preferable to:

```csharp
repo<Person>.Include(...).ThenInclude(...).InDepartment(...).Paging(...);
```

because:

- the repository stays small and reusable
- EF Core translation remains transparent
- feature-specific filters are discoverable next to the entity they belong to
- testability is better because query logic is decomposed into small units

## Aggregate Query Extensions

Put aggregate-specific query logic in extension classes such as `PersonQueryExtensions`.

```csharp
public static class PersonQueryExtensions
{
    public static IQueryable<Person> WithJobsAndDepartments(this IQueryable<Person> query)
    {
        return query
            .Include(person => person.Jobs)
            .ThenInclude(job => job.Department);
    }

    public static IQueryable<Person> InDepartment(this IQueryable<Person> query, string departmentName)
    {
        if (string.IsNullOrWhiteSpace(departmentName))
        {
            return query;
        }

        return query.Where(person =>
            person.Jobs.Any(job => job.Department.Name == departmentName));
    }

    public static IQueryable<Person> MinAge(this IQueryable<Person> query, int minAge)
    {
        return query.Where(person => person.Age >= minAge);
    }

    public static IOrderedQueryable<Person> OrderByLastThenFirst(this IQueryable<Person> query)
    {
        return query
            .OrderBy(person => person.LastName)
            .ThenBy(person => person.FirstName);
    }
}
```

If the module wants these filters on the repository contract as well, the repository can delegate to the same extensions:

```csharp
public sealed class PersonRepository
    : BaseEfCommandRepository<Person, Guid>, IPersonRepository
{
    public PersonRepository(PeopleDbContext dbContext) : base(dbContext)
    {
    }

    public override Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Query()
            .FirstOrDefaultAsync(person => person.Id == id, cancellationToken);
    }

    public Task<Person?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return Query()
            .FirstOrDefaultAsync(person => person.Email == email, cancellationToken);
    }

    public IQueryable<Person> WithJobsAndDepartments()
    {
        return Query().WithJobsAndDepartments();
    }

    public IQueryable<Person> InDepartment(string departmentName)
    {
        return Query().InDepartment(departmentName);
    }

    public IQueryable<Person> MinAge(int minAge)
    {
        return Query().MinAge(minAge);
    }
}
```

This keeps the actual filter logic in one place while still allowing the module contract to advertise its important query entry points.

## Shared Cross-Cutting Query Extensions

Keep generic concerns reusable and separate from entity-specific rules.

Examples:

- paging
- conditional filtering
- optional tracking
- projection helpers

```csharp
public static class QueryableExtensions
{
    public static IQueryable<T> Page<T>(this IQueryable<T> query, int page, int size)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedSize = size < 1 ? 20 : size;

        return query
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize);
    }
}
```

## Specifications For Reusable Query Bundles

When a query becomes too large for a few extensions, move it into a specification object instead of adding more repository methods.

```csharp
public interface IQuerySpecification<TEntity>
    where TEntity : class
{
    IQueryable<TEntity> Apply(IQueryable<TEntity> query);
}
```

Example:

```csharp
public sealed class ActiveSalesPeopleSpecification : IQuerySpecification<Person>
{
    private readonly int _minAge;

    public ActiveSalesPeopleSpecification(int minAge)
    {
        _minAge = minAge;
    }

    public IQueryable<Person> Apply(IQueryable<Person> query)
    {
        return query
            .WithJobsAndDepartments()
            .InDepartment("Sales")
            .MinAge(_minAge)
            .OrderByLastThenFirst();
    }
}
```

Optional repository helper:

```csharp
public static class QuerySpecificationExtensions
{
    public static IQueryable<TEntity> Apply<TEntity>(
        this IQueryable<TEntity> query,
        IQuerySpecification<TEntity> specification)
        where TEntity : class
    {
        return specification.Apply(query);
    }
}
```

Usage:

```csharp
var specification = new ActiveSalesPeopleSpecification(minAge: 18);

var people = await personRepository
    .Query()
    .Apply(specification)
    .Page(page: 3, size: 20)
    .ToListAsync(cancellationToken);
```

## Ordering Guidance

Avoid a generic pattern like:

```csharp
OrderBy(person => new { first = person.LastName, second = person.FirstName })
```

as the default shared convention.

Prefer one of these:

- entity-specific ordering helpers such as `OrderByLastThenFirst()`
- explicit `OrderBy(...).ThenBy(...)` chains
- a small sorting abstraction when the sort options are user-driven

This is clearer and less likely to cause awkward translation or hidden complexity.

## Module and Service Boundaries

- The shared repository library should contain only generic abstractions and base infrastructure helpers.
- Each module should own its `IEntityRepository` contract and concrete repository implementation.
- Aggregate-specific query extensions should live in the module or service that owns the aggregate.
- Do not place business-specific filters for many unrelated aggregates into the shared library.
- In a microservice system, each service should still own its own concrete repositories and data model.

## Testing Expectations

- Unit test entity-specific query extensions when they contain meaningful filter logic.
- Integration test repository queries against the actual EF Core provider used in the application when translation details matter.
- Add architecture tests when needed to keep shared abstractions from absorbing business logic.

## Avoid

- Repositories with dozens of custom methods for every read variation.
- Hiding `IQueryable` completely and forcing all query composition into repository classes.
- Putting aggregate-specific include graphs into a global shared library.
- Treating repository base classes as the place for business rules.
- Building a custom LINQ replacement when simple extensions or specifications are enough.

## Recommended Default

Default to this pattern:

- Generic repository exposes `Query()`
- Base EF repositories handle only persistence plumbing
- Aggregate modules own `IQueryable<TEntity>` extension methods
- Specifications are added only when reuse or complexity justifies them

This gives a concise and elegant query style without fighting EF Core.
