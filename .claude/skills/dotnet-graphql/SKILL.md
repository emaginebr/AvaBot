---
name: dotnet-graphql
description: "Guides implementation of GraphQL with HotChocolate in .NET 8 projects following Clean Architecture. Creates the GraphQL project with single or multi-schema design, queries returning IQueryable for EF Core optimization, type extensions for computed fields, field hiding, error logging, and DI registration. Use when adding GraphQL support, creating queries, type extensions, or configuring GraphQL schemas."
allowed-tools: Read, Grep, Glob, Bash, Write, Edit, Task
---

# .NET GraphQL Implementation Guide (HotChocolate)

You are an expert assistant that helps developers implement GraphQL using HotChocolate in .NET 8 projects following Clean Architecture. You guide the user through creating the GraphQL layer with IQueryable-based queries, type extensions, and proper DI registration. The schema design is flexible — single schema or multiple schemas depending on the project's needs.

## Input

The user will describe what to create or modify: `$ARGUMENTS`

Before generating code:
1. **Read the solution structure** — Identify the project name prefix (e.g., `Lofn`, `MyApp`)
2. **Read the DbContext** — Find all `DbSet<T>` entities available for GraphQL exposure
3. **Read existing GraphQL files** (if any) — Match existing patterns exactly
4. **Read the Startup/DI setup** — Find the centralized DI class (usually `Application/Startup.cs`)
5. **Identify the auth setup** — Find how authentication works (middleware, bearer token, etc.)
6. **Identify entity relationships** — Understand navigation properties for type extensions
7. **Determine schema strategy** — Ask or infer whether the project needs single schema, dual schema (public + admin), or custom named schemas

---

## Architecture Overview

### Project Structure

```
{Project}.GraphQL/
├── GraphQLServiceExtensions.cs     ← DI registration, configures schema(s)
├── GraphQLErrorLogger.cs           ← Diagnostic event listener for logging
├── {Project}.GraphQL.csproj        ← NuGet dependencies
├── Queries/
│   └── {Entity}Query.cs            ← Query class(es) — single or split by access level
├── Types/
│   ├── {Entity}TypeExtension.cs    ← Computed fields via ObjectTypeExtension
│   └── {Entity}Type.cs             ← ObjectType with field hiding (if needed)
└── (optional) Public/ & Admin/     ← Only if using multi-schema design
```

### Dependency Flow

```
{Project}.API (Startup) → {Project}.GraphQL → {Project}.Infra (DbContext, Entities)
                                             → {Project}.Domain (Interfaces)
```

### Schema Strategy

Choose based on project requirements:

| Strategy | When to use | Structure |
|----------|-------------|-----------|
| **Single schema** | All queries require auth, or no auth distinction needed | One `Query` class, one `AddGraphQLServer()` |
| **Dual schema** (public + admin) | Some queries are anonymous, others require auth with different data visibility | `PublicQuery` + `AdminQuery`, two `AddGraphQLServer()` calls |
| **Multiple named schemas** | Complex projects with distinct API consumers | Multiple named servers with separate query types |

---

## Step-by-Step Implementation

### Step 1: Create the GraphQL Project

```bash
dotnet new classlib -n {Project}.GraphQL -f net8.0
dotnet sln add {Project}.GraphQL
```

### Step 2: Add NuGet Packages

```xml
<!-- {Project}.GraphQL.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="HotChocolate.AspNetCore" Version="14.3.0" />
    <PackageReference Include="HotChocolate.AspNetCore.Authorization" Version="14.3.0" />
    <PackageReference Include="HotChocolate.Data.EntityFramework" Version="14.3.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\{Project}.Domain\{Project}.Domain.csproj" />
    <ProjectReference Include="..\{Project}.Infra\{Project}.Infra.csproj" />
  </ItemGroup>

</Project>
```

### Step 3: Error Logger

Create `GraphQLErrorLogger.cs` — centralized error handling for all GraphQL operations.

```csharp
using System;
using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using Microsoft.Extensions.Logging;

namespace {Project}.GraphQL;

public class GraphQLErrorLogger : ExecutionDiagnosticEventListener
{
    private readonly ILogger<GraphQLErrorLogger> _logger;

    public GraphQLErrorLogger(ILogger<GraphQLErrorLogger> logger)
    {
        _logger = logger;
    }

    public override void RequestError(IRequestContext context, Exception exception)
    {
        _logger.LogError(exception, "GraphQL request error");
    }

    public override void ValidationErrors(IRequestContext context, IReadOnlyList<IError> errors)
    {
        foreach (var error in errors)
            _logger.LogWarning("GraphQL validation error: {Message}", error.Message);
    }

    public override void SyntaxError(IRequestContext context, IError error)
    {
        _logger.LogWarning("GraphQL syntax error: {Message}", error.Message);
    }

    public override void ResolverError(IMiddlewareContext context, IError error)
    {
        _logger.LogError(error.Exception, "GraphQL resolver error on {Path}: {Message}", context.Path, error.Message);
    }

    public override void ResolverError(IRequestContext context, ISelection selection, IError error)
    {
        _logger.LogError(error.Exception, "GraphQL resolver error on {Field}: {Message}", selection.Field.Name, error.Message);
    }
}
```

### Step 4: Query Classes

Queries always return `IQueryable<T>` for EF Core optimization. The structure depends on the schema strategy.

#### Single Schema — One Query class

```csharp
using System.Linq;
using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using HotChocolate.Types;
using {Project}.Infra.Context;
using Microsoft.AspNetCore.Http;

namespace {Project}.GraphQL.Queries;

public class Query
{
    /// <summary>
    /// List query with pagination, filtering, and sorting.
    /// </summary>
    [UseOffsetPaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<{Entity}> Get{Entity}s({DbContext} context)
        => context.{Entity}s.Where(e => e.Status == 1);

    /// <summary>
    /// Single record lookup by unique field (no pagination needed).
    /// </summary>
    [UseProjection]
    public IQueryable<{Entity}> Get{Entity}BySlug({DbContext} context, string slug)
        => context.{Entity}s.Where(e => e.Status == 1 && e.Slug == slug);

    /// <summary>
    /// Authenticated query — use [Authorize] at method level for mixed schemas.
    /// </summary>
    [Authorize]
    [UseOffsetPaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<{Entity}> GetMy{Entity}s(
        {DbContext} context,
        IHttpContextAccessor httpContextAccessor,
        [Service] {IUserService} userService)
    {
        var user = userService.GetCurrentUser(httpContextAccessor.HttpContext!);
        return context.{Entity}s.Where(e => e.OwnerId == user!.UserId);
    }
}
```

#### Dual Schema — Public + Admin (split query classes)

**Public (anonymous):**

```csharp
using System.Linq;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using {Project}.Infra.Context;

namespace {Project}.GraphQL.Public;

public class PublicQuery
{
    [UseOffsetPaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<{Entity}> Get{Entity}s({DbContext} context)
        => context.{Entity}s.Where(e => e.Status == 1);

    [UseProjection]
    public IQueryable<{Entity}> Get{Entity}BySlug({DbContext} context, string slug)
        => context.{Entity}s.Where(e => e.Status == 1 && e.Slug == slug);
}
```

**Admin (authenticated, user-scoped):**

```csharp
using System.Linq;
using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using HotChocolate.Types;
using {Project}.Infra.Context;
using Microsoft.AspNetCore.Http;

namespace {Project}.GraphQL.Admin;

[Authorize]
public class AdminQuery
{
    private IQueryable<long> GetUserEntityIds(
        {DbContext} context,
        IHttpContextAccessor httpContextAccessor,
        {IUserService} userService)
    {
        var user = userService.GetCurrentUser(httpContextAccessor.HttpContext!);
        var userId = user!.UserId;
        return context.{UserEntityMapping}s
            .Where(m => m.UserId == userId)
            .Select(m => m.{EntityId});
    }

    [UseOffsetPaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<{Entity}> GetMy{Entity}s(
        {DbContext} context,
        IHttpContextAccessor httpContextAccessor,
        [Service] {IUserService} userService)
    {
        var entityIds = GetUserEntityIds(context, httpContextAccessor, userService);
        return context.{Entity}s.Where(e => entityIds.Contains(e.{EntityId}));
    }
}
```

**Critical patterns:**
- **Always return `IQueryable<T>`** — never `List<T>` or `Task<List<T>>`. HotChocolate builds optimized SQL via EF Core
- **DbContext is injected as parameter** — HotChocolate resolves it automatically from DI
- **Attribute order matters:** `[UseOffsetPaging]` → `[UseProjection]` → `[UseFiltering]` → `[UseSorting]`
- **`[Authorize]`** — at class level (all queries require auth) or method level (mixed access)
- **`[Service]` attribute** — for custom interfaces not auto-resolved by HotChocolate
- **User scoping** — `GetUserEntityIds()` returns `IQueryable<long>` for SQL subquery composition (no materialization)

### Step 5: Field Hiding (ObjectType)

Create an `ObjectType<T>` to hide sensitive fields. Used when certain fields should not be exposed in a schema.

```csharp
using HotChocolate.Types;
using {Project}.Infra.Context;

namespace {Project}.GraphQL.Types;

public class {Entity}Type : ObjectType<{Entity}>
{
    protected override void Configure(IObjectTypeDescriptor<{Entity}> descriptor)
    {
        descriptor.Ignore(e => e.OwnerId);
        descriptor.Ignore(e => e.{SensitiveNavigationProperty});
    }
}
```

**Guidelines:**
- Hide: owner IDs, internal user associations, internal status fields, audit fields
- In dual-schema: register ONLY on the schema that needs hiding (e.g., public but not admin)
- In single schema: register globally — the fields are hidden for everyone

### Step 6: Type Extensions (Computed Fields)

Create files in `Types/` — add derived/computed fields to entities without modifying the entity model.

#### Pattern A: ObjectTypeExtension with inline resolver (for field transformations, async lookups)

```csharp
using HotChocolate.Types;
using {Project}.Infra.Context;

namespace {Project}.GraphQL.Types;

public class {Entity}TypeExtension : ObjectTypeExtension<{Entity}>
{
    protected override void Configure(IObjectTypeDescriptor<{Entity}> descriptor)
    {
        // Ensure the source field is included in SQL projection
        descriptor.Field(t => t.{SourceField}).IsProjected(true);

        descriptor
            .Field("{computedFieldName}")
            .Type<StringType>()
            .Resolve(async ctx =>
            {
                var entity = ctx.Parent<{Entity}>();
                if (string.IsNullOrEmpty(entity.{SourceField})) return null;

                var service = ctx.Service<{IService}>();
                return await service.{Method}(entity.{SourceField});
            });
    }
}
```

#### Pattern B: ObjectTypeExtension with database lookup (for related entity data)

```csharp
using System.Linq;
using HotChocolate.Types;
using {Project}.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace {Project}.GraphQL.Types;

public class {Entity}TypeExtension : ObjectTypeExtension<{Entity}>
{
    protected override void Configure(IObjectTypeDescriptor<{Entity}> descriptor)
    {
        descriptor
            .Field("{computedFieldName}")
            .Type<StringType>()
            .Resolve(async ctx =>
            {
                var entity = ctx.Parent<{Entity}>();
                var dbContext = ctx.Service<{DbContext}>();

                var result = await dbContext.{RelatedEntities}
                    .Where(r => r.{EntityId} == entity.{EntityId})
                    .OrderBy(r => r.{SortField})
                    .Select(r => r.{TargetField})
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(result)) return null;

                var service = ctx.Service<{IService}>();
                return await service.{Method}(result);
            });
    }
}
```

#### Pattern C: ExtendObjectType attribute with method resolver (for simple computed fields)

```csharp
using System.Linq;
using HotChocolate;
using HotChocolate.Types;
using {Project}.Infra.Context;

namespace {Project}.GraphQL.Types;

[ExtendObjectType(typeof({Entity}))]
public class {Entity}TypeExtension
{
    public int Get{ComputedField}(
        [Parent] {Entity} entity,
        [Service] {DbContext} context)
    {
        return context.{RelatedEntities}.Count(r => r.{EntityId} == entity.{EntityId} && r.Status == 1);
    }
}
```

**When to use each pattern:**
- **Pattern A** — Transform a field value (e.g., filename → URL). Use `IsProjected(true)` to ensure the source field is in SQL
- **Pattern B** — Computed field requires querying a related table not available via navigation property
- **Pattern C** — Simple synchronous or single-expression computed fields. Uses `[Parent]` and `[Service]` attributes

**Key elements:**
- **`.IsProjected(true)`** — ensures the source field is included in SQL even if not in the GraphQL query
- **`ctx.Parent<T>()`** — accesses the parent entity being extended
- **`ctx.Service<T>()`** — resolves a service from DI container
- **Null check** — always guard before resolving

### Step 7: DI Registration (GraphQLServiceExtensions)

Create `GraphQLServiceExtensions.cs` — the configuration varies by schema strategy.

#### Single Schema

```csharp
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Options;
using HotChocolate.Types.Pagination;
using {Project}.GraphQL.Queries;
using {Project}.GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace {Project}.GraphQL;

public static class GraphQLServiceExtensions
{
    public static IServiceCollection Add{Project}GraphQL(this IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddAuthorization()
            .AddDiagnosticEventListener<GraphQLErrorLogger>()
            .AddQueryType<Query>()
            .AddType<{Entity}Type>()                    // Field hiding (if needed)
            .AddTypeExtension<{Entity}TypeExtension>()  // Computed fields
            .SetPagingOptions(new PagingOptions
            {
                MaxPageSize = 50,
                DefaultPageSize = 10,
                IncludeTotalCount = true
            })
            .AddProjections()
            .AddFiltering()
            .AddSorting()
            .ModifyCostOptions(o => o.MaxFieldCost = 8000);

        return services;
    }
}
```

#### Dual Schema (Public + Admin)

```csharp
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Options;
using HotChocolate.Types.Pagination;
using {Project}.GraphQL.Admin;
using {Project}.GraphQL.Public;
using {Project}.GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace {Project}.GraphQL;

public static class GraphQLServiceExtensions
{
    public static IServiceCollection Add{Project}GraphQL(this IServiceCollection services)
    {
        // ── Public Server (anonymous access) ──
        services
            .AddGraphQLServer()
            .AddAuthorization()
            .AddDiagnosticEventListener<GraphQLErrorLogger>()
            .AddQueryType<PublicQuery>()
            .AddType<{Entity}Type>()                    // Field hiding (only on public)
            .AddTypeExtension<{Entity}TypeExtension>()  // Computed fields
            .SetPagingOptions(new PagingOptions
            {
                MaxPageSize = 50,
                DefaultPageSize = 10,
                IncludeTotalCount = true
            })
            .AddProjections()
            .AddFiltering()
            .AddSorting()
            .ModifyCostOptions(o => o.MaxFieldCost = 8000);

        // ── Admin Server (authenticated access) ──
        services
            .AddGraphQLServer("admin")
            .AddAuthorization()
            .AddDiagnosticEventListener<GraphQLErrorLogger>()
            .AddQueryType<AdminQuery>()
            // NO {Entity}Type here — admin sees all fields
            .AddTypeExtension<{Entity}TypeExtension>()  // Same computed fields
            .SetPagingOptions(new PagingOptions
            {
                MaxPageSize = 50,
                DefaultPageSize = 10,
                IncludeTotalCount = true
            })
            .AddProjections()
            .AddFiltering()
            .AddSorting()
            .ModifyCostOptions(o => o.MaxFieldCost = 8000);

        return services;
    }
}
```

**Key configuration:**
- **`AddAuthorization()`** — enables `[Authorize]` attribute support
- **`AddDiagnosticEventListener<GraphQLErrorLogger>()`** — connects error logging
- **`AddType<{Entity}Type>()`** — field hiding (register only where needed)
- **Type extensions** — registered on all schemas that need computed fields
- **Paging options** — `MaxPageSize=50`, `DefaultPageSize=10`, `IncludeTotalCount=true`
- **`AddProjections()` + `AddFiltering()` + `AddSorting()`** — HotChocolate EF Core integration
- **`ModifyCostOptions`** — `MaxFieldCost = 8000` prevents expensive nested queries

### Step 8: API Startup — Endpoint Mapping

Add to the API's `Startup.cs`. Configuration varies by schema strategy.

#### Single Schema

```csharp
// ConfigureServices:
services.Add{Project}GraphQL();

// Configure (middleware pipeline):
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapGraphQL("/graphql");  // Single endpoint
});
```

#### Dual Schema

```csharp
// ConfigureServices:
services.Add{Project}GraphQL();

// Configure (middleware pipeline):
// Authentication runs conditionally — skip for public endpoint
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/graphql")
               || context.Request.Path.StartsWithSegments("/graphql/admin"),
    branch => branch.UseAuthentication()
);

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapGraphQL("/graphql").AllowAnonymous();       // Public schema
    endpoints.MapGraphQL("/graphql/admin", "admin");         // Admin schema
});
```

All endpoints expose the **Banana Cake Pop** interactive playground for development.

### Step 9: Add Project Reference

Add the GraphQL project reference to `{Project}.Application.csproj`:

```xml
<ProjectReference Include="..\{Project}.GraphQL\{Project}.GraphQL.csproj" />
```

And call `Add{Project}GraphQL()` from the centralized `Startup.cs` in Application.

---

## Adding New Elements

### To add a query:

Add a method to the query class:
```csharp
[UseOffsetPaging]
[UseProjection]
[UseFiltering]
[UseSorting]
public IQueryable<{Entity}> Get{Entity}s({DbContext} context)
    => context.{Entity}s.Where(e => e.Status == 1);
```

For authenticated queries, add `[Authorize]` (method level for single schema, class level for dedicated admin query class).

### To add a type extension:

1. Create `Types/{Entity}TypeExtension.cs` using Pattern A, B, or C
2. Register in `GraphQLServiceExtensions.cs`:
```csharp
.AddTypeExtension<{Entity}TypeExtension>()
```

### To add field hiding:

1. Create `Types/{Entity}Type.cs` with `descriptor.Ignore()`
2. Register in `GraphQLServiceExtensions.cs`:
```csharp
.AddType<{Entity}Type>()
```

### To add a new named schema:

```csharp
services
    .AddGraphQLServer("{schemaName}")
    .AddQueryType<{SchemaName}Query>()
    // ... same configuration chain

// Endpoint:
endpoints.MapGraphQL("/graphql/{schemaName}", "{schemaName}");
```

---

## Checklist

| # | File | Action | Description |
|---|------|--------|-------------|
| 1 | `{Project}.GraphQL.csproj` | Create | Project with HotChocolate packages |
| 2 | `GraphQLErrorLogger.cs` | Create | Diagnostic event listener for error logging |
| 3 | Query class(es) | Create | `Query.cs` (single) or `PublicQuery.cs` + `AdminQuery.cs` (dual) |
| 4 | `Types/{Entity}Type.cs` | Create | ObjectType with field hiding (if needed) |
| 5 | `Types/{Entity}TypeExtension.cs` | Create | Computed fields via type extensions (one per entity) |
| 6 | `GraphQLServiceExtensions.cs` | Create | DI registration for schema(s) |
| 7 | `Application/Startup.cs` | Modify | Call `Add{Project}GraphQL()` |
| 8 | `API/Startup.cs` | Modify | Add endpoint mapping and auth middleware |
| 9 | `Application .csproj` | Modify | Add project reference to GraphQL project |

---

## Response Guidelines

1. **Read existing files first** — If the GraphQL project already exists, match its patterns exactly
2. **Read the DbContext** — Identify all `DbSet<T>` entities and their relationships
3. **Determine schema strategy** — Single or multi-schema based on the project's auth and access needs
4. **Always return `IQueryable<T>`** — Never materialize queries in GraphQL resolvers
5. **Use attribute order** — `[UseOffsetPaging]` → `[UseProjection]` → `[UseFiltering]` → `[UseSorting]`
6. **Register type extensions on all relevant schemas** — Computed fields should be available where needed
7. **Use `IsProjected(true)`** — When a computed field depends on a source field
8. **Match the project's auth pattern** — Check how bearer tokens and user sessions are resolved
9. **Check for null values** — Always guard against null in resolvers before calling services
10. **Don't force dual-schema** — Use single schema when there's no need for separate public/admin access
