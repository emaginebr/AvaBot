---
name: dotnet-multi-tenant
description: "Implements Multi-Tenant pattern in .NET APIs with per-tenant database isolation, per-tenant JWT secrets, ACL layer with automatic TenantId propagation via DelegatingHandler, and dynamic DbContext factory. Covers TenantMiddleware, TenantContext, TenantResolver, TenantHeaderHandler, TenantDbContextFactory, and dynamic JWT configuration."
allowed-tools: Read, Grep, Glob, Bash, Write, Edit, Task
---

# .NET Multi-Tenant API Implementation Guide

You are an expert assistant that helps developers implement the **Multi-Tenant** pattern in .NET APIs. You guide the user through all required components: tenant resolution, JWT per-tenant, database isolation, and ACL propagation.

## Input

The user will describe what to create or modify: `$ARGUMENTS`

Before generating code:
1. **Read the solution structure** — Identify all projects/layers (API, ACL, Application, Domain, Infra)
2. **Find existing patterns** — Check for existing authentication, DbContext, and HttpClient registrations
3. **Read `appsettings.json`** — Check for existing tenant configuration
4. **Read `Program.cs` or `Startup.cs`** — Understand current DI and middleware setup
5. **Identify existing DbContext** — You must NOT modify Models, DTOs, or DbContext structure

---

## Architecture Overview

### Multi-Tenant Resolution Flow

```
┌─────────────────────────────────────────────────────────┐
│                      API Layer                          │
│                                                         │
│  ┌─────────────────┐    ┌────────────────────────────┐  │
│  │ TenantMiddleware │───▶│ HttpContext.Items["TenantId"]│ │
│  │ (runs before     │    └────────────────────────────┘  │
│  │  auth pipeline)  │                                    │
│  └─────────────────┘                                    │
│                                                         │
│  Unauthenticated endpoints → Header: X-Tenant-Id       │
│  Authenticated endpoints   → JWT claim: tenant_id       │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                      ACL Layer                          │
│                                                         │
│  ┌──────────────────────┐   ┌────────────────────────┐  │
│  │ TenantHeaderHandler   │──▶│ Injects X-Tenant-Id    │  │
│  │ (DelegatingHandler)   │   │ into ALL HttpClient    │  │
│  │                       │   │ requests automatically │  │
│  └──────────────────────┘   └────────────────────────┘  │
│                                                         │
│  TenantId source: appsettings.json → Tenant:DefaultId   │
│  NEVER passed as method parameter                       │
└─────────────────────────────────────────────────────────┘
```

### Per-Tenant Isolation

| Concern | Strategy |
|---|---|
| Database | Separate ConnectionString per tenant |
| Authentication | Separate JwtSecret per tenant |
| Tenant Resolution (API) | Header or JWT claim |
| Tenant Resolution (ACL) | `appsettings.json` config |

---

## Configuration — `appsettings.json`

Each tenant has its own `ConnectionString` and `JwtSecret`:

```json
{
  "Tenant": {
    "DefaultTenantId": "tenant-a"
  },
  "Tenants": {
    "tenant-a": {
      "ConnectionString": "Server=srv1;Database=TenantA_DB;User Id=sa;Password=***;",
      "JwtSecret": "super-secret-key-tenant-a-256bits"
    },
    "tenant-b": {
      "ConnectionString": "Server=srv2;Database=TenantB_DB;User Id=sa;Password=***;",
      "JwtSecret": "super-secret-key-tenant-b-256bits"
    }
  }
}
```

---

## Components to Implement

### 1. ITenantContext / TenantContext (Application Layer)

Provides the current tenant ID within the API request scope.

**File:** `Application/Interfaces/ITenantContext.cs`

```csharp
public interface ITenantContext
{
    string TenantId { get; }
}
```

**File:** `Application/Services/TenantContext.cs`

```csharp
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string TenantId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                throw new InvalidOperationException("No active HTTP context.");

            // Authenticated: resolve from JWT claim
            var claimTenant = context.User?.FindFirst("tenant_id")?.Value;
            if (!string.IsNullOrEmpty(claimTenant))
                return claimTenant;

            // Unauthenticated: resolve from middleware-set item
            if (context.Items.TryGetValue("TenantId", out var headerTenant)
                && headerTenant is string tenantStr
                && !string.IsNullOrEmpty(tenantStr))
                return tenantStr;

            throw new InvalidOperationException(
                "TenantId could not be resolved from JWT claim or X-Tenant-Id header.");
        }
    }
}
```

**Registration:** `Scoped`

---

### 2. TenantMiddleware (API Layer)

Runs **before** the authentication middleware. Extracts `X-Tenant-Id` from the request header and stores it in `HttpContext.Items`.

**File:** `Api/Middlewares/TenantMiddleware.cs`

```csharp
public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId)
            && !string.IsNullOrWhiteSpace(tenantId))
        {
            context.Items["TenantId"] = tenantId.ToString();
        }

        await _next(context);
    }
}
```

**Registration in `Program.cs`:**

```csharp
// MUST be registered BEFORE UseAuthentication / UseAuthorization
app.UseMiddleware<TenantMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
```

---

### 3. ITenantResolver / TenantResolver (ACL Layer)

Resolves tenant configuration from `appsettings.json`. Used by the ACL layer only.

**File:** `ACL/Interfaces/ITenantResolver.cs`

```csharp
public interface ITenantResolver
{
    string TenantId { get; }
    string ConnectionString { get; }
    string JwtSecret { get; }
}
```

**File:** `ACL/Services/TenantResolver.cs`

```csharp
public class TenantResolver : ITenantResolver
{
    private readonly IConfiguration _configuration;

    public TenantResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string TenantId
    {
        get
        {
            var tenantId = _configuration["Tenant:DefaultTenantId"];
            if (string.IsNullOrEmpty(tenantId))
                throw new InvalidOperationException(
                    "Tenant:DefaultTenantId is not configured in appsettings.json.");
            return tenantId;
        }
    }

    public string ConnectionString
    {
        get
        {
            var cs = _configuration[$"Tenants:{TenantId}:ConnectionString"];
            if (string.IsNullOrEmpty(cs))
                throw new InvalidOperationException(
                    $"ConnectionString not found for tenant '{TenantId}'. " +
                    $"Expected key: Tenants:{TenantId}:ConnectionString");
            return cs;
        }
    }

    public string JwtSecret
    {
        get
        {
            var secret = _configuration[$"Tenants:{TenantId}:JwtSecret"];
            if (string.IsNullOrEmpty(secret))
                throw new InvalidOperationException(
                    $"JwtSecret not found for tenant '{TenantId}'. " +
                    $"Expected key: Tenants:{TenantId}:JwtSecret");
            return secret;
        }
    }
}
```

**Registration:** `Scoped`

---

### 4. TenantHeaderHandler (ACL Layer)

A `DelegatingHandler` that automatically injects the `X-Tenant-Id` header into **every** HTTP request made by ACL `HttpClient` instances. The `TenantId` is **never** passed as a method parameter.

**File:** `ACL/Handlers/TenantHeaderHandler.cs`

```csharp
public class TenantHeaderHandler : DelegatingHandler
{
    private readonly IConfiguration _configuration;

    public TenantHeaderHandler(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tenantId = _configuration["Tenant:DefaultTenantId"];
        if (!string.IsNullOrEmpty(tenantId))
            request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);

        return await base.SendAsync(request, cancellationToken);
    }
}
```

**Registration:** `Transient`

```csharp
services.AddTransient<TenantHeaderHandler>();

// Register for EVERY ACL HttpClient:
services.AddHttpClient<IMyAclService, MyAclService>()
    .AddHttpMessageHandler<TenantHeaderHandler>();
```

---

### 5. Dynamic JWT Configuration

Since each tenant has its own `JwtSecret`, JWT validation **cannot** use a static key. Use `IssuerSigningKeyResolver` to dynamically resolve the signing key based on the `tenant_id` claim in the token.

**JWT Validation Setup (in `Program.cs` or auth config):**

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);
                var tenantId = jwt.Claims
                    .FirstOrDefault(c => c.Type == "tenant_id")?.Value;

                var secret = configuration[$"Tenants:{tenantId}:JwtSecret"];
                if (string.IsNullOrEmpty(secret))
                    throw new SecurityTokenException(
                        $"JwtSecret not found for tenant: {tenantId}");

                return new[]
                {
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
                };
            },
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });
```

**Token Generation (Login):**

1. Resolve `TenantId` from `X-Tenant-Id` header
2. Read `JwtSecret` via `IConfiguration["Tenants:{tenantId}:JwtSecret"]`
3. Include `tenant_id` claim in the payload
4. Sign with the tenant's `JwtSecret`

```csharp
public interface ITokenService
{
    string GenerateToken(string tenantId, string userId, string email);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(string tenantId, string userId, string email)
    {
        var secret = _configuration[$"Tenants:{tenantId}:JwtSecret"]
            ?? throw new InvalidOperationException(
                $"JwtSecret not found for tenant: {tenantId}");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("tenant_id", tenantId),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

---

### 6. ITenantDbContextFactory / TenantDbContextFactory (ACL Layer)

Creates `DbContext` instances at runtime using the tenant-specific `ConnectionString`. Does **NOT** modify the existing `AppDbContext`.

**File:** `ACL/Interfaces/ITenantDbContextFactory.cs`

```csharp
public interface ITenantDbContextFactory
{
    AppDbContext CreateDbContext();
}
```

**File:** `ACL/Services/TenantDbContextFactory.cs`

```csharp
public class TenantDbContextFactory : ITenantDbContextFactory
{
    private readonly ITenantResolver _tenantResolver;

    public TenantDbContextFactory(ITenantResolver tenantResolver)
    {
        _tenantResolver = tenantResolver;
    }

    public AppDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(_tenantResolver.ConnectionString);
        return new AppDbContext(optionsBuilder.Options);
    }
}
```

**Registration:** `Scoped`

---

### 7. Dependency Injection — Full Registration

```csharp
// --- HTTP Context ---
services.AddHttpContextAccessor();

// --- Tenant Services ---
services.AddScoped<ITenantContext, TenantContext>();
services.AddScoped<ITenantResolver, TenantResolver>();
services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();
services.AddScoped<ITokenService, TokenService>();

// --- TenantHeaderHandler ---
services.AddTransient<TenantHeaderHandler>();

// --- DbContext via Factory ---
services.AddScoped(sp =>
    sp.GetRequiredService<ITenantDbContextFactory>().CreateDbContext());

// --- ACL HttpClients (add handler to ALL) ---
services.AddHttpClient<IMyAclService, MyAclService>()
    .AddHttpMessageHandler<TenantHeaderHandler>();

// Repeat for every ACL HttpClient:
// services.AddHttpClient<IOtherAclService, OtherAclService>()
//     .AddHttpMessageHandler<TenantHeaderHandler>();

// --- JWT Authentication ---
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* IssuerSigningKeyResolver as shown above */ });
```

---

## Expected Folder Structure

```
src/
├── Api/
│   ├── Middlewares/
│   │   └── TenantMiddleware.cs
│   └── Program.cs
├── ACL/
│   ├── Handlers/
│   │   └── TenantHeaderHandler.cs
│   ├── Interfaces/
│   │   ├── ITenantResolver.cs
│   │   └── ITenantDbContextFactory.cs
│   └── Services/
│       ├── TenantResolver.cs
│       └── TenantDbContextFactory.cs
└── Application/
    ├── Interfaces/
    │   └── ITenantContext.cs
    └── Services/
        └── TenantContext.cs
```

---

## Mandatory Constraints

1. **Do NOT modify** Models, DTOs, `DbSet`s, or `OnModelCreating` in the existing `AppDbContext`
2. **Do NOT use** static `AddDbContext` — use the scoped factory instead
3. **Do NOT use** a global/static `JwtSecret` — each tenant has its own
4. **In the ACL, NEVER pass `TenantId` as a method parameter** — propagate exclusively via `X-Tenant-Id` header through `TenantHeaderHandler`
5. **Security**: never accept `TenantId` from request body — only from header or JWT
6. **Security**: in `IssuerSigningKeyResolver`, only read the token to extract `tenant_id` — signature validation occurs immediately after with the resolved key
7. **ACL**: do not directly reference infrastructure projects — use interfaces
8. **Lifecycle**: tenant services are `Scoped`; `TenantHeaderHandler` is `Transient`

---

## Usage Examples

### ACL Service — No tenant parameter; header injected automatically

```csharp
public class ProductAclService : IProductAclService
{
    private readonly HttpClient _httpClient;

    public ProductAclService(HttpClient httpClient)
    {
        _httpClient = httpClient; // TenantHeaderHandler already adds X-Tenant-Id
    }

    public async Task<List<ProductDto>> GetAllAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<ProductDto>>("api/products");
    }
}
```

### API — Unauthenticated endpoint

```csharp
[AllowAnonymous]
[HttpGet("public/products")]
public async Task<IActionResult> GetPublicProducts()
{
    // TenantId resolved from X-Tenant-Id header
    var data = await _service.GetAllAsync();
    return Ok(data);
}
```

### API — Authenticated endpoint

```csharp
[Authorize]
[HttpGet("private/products")]
public async Task<IActionResult> GetPrivateProducts()
{
    // TenantId resolved from JWT claim
    var data = await _service.GetAllAsync();
    return Ok(data);
}
```

---

## Deliverables Checklist

- [ ] `TenantMiddleware.cs`
- [ ] `ITenantContext.cs` and `TenantContext.cs`
- [ ] `ITenantResolver.cs` and `TenantResolver.cs` with `TenantId`, `ConnectionString`, and `JwtSecret`
- [ ] `TenantHeaderHandler.cs` — DelegatingHandler injecting `X-Tenant-Id` into all ACL HttpClients
- [ ] `ITenantDbContextFactory.cs` and `TenantDbContextFactory.cs`
- [ ] `AddJwtBearer` configuration with dynamic `IssuerSigningKeyResolver`
- [ ] Token generation service (`ITokenService` / `TokenService`) signing with the tenant's `JwtSecret`
- [ ] Full DI registration in `Program.cs` including `AddHttpMessageHandler<TenantHeaderHandler>()` for all ACL HttpClients
- [ ] `appsettings.json` with unified `Tenants` structure
- [ ] Unit test example mocking `ITenantResolver` and `TenantHeaderHandler`

---

## Unit Test Example

```csharp
[Fact]
public void TenantResolver_ShouldReturn_CorrectConnectionString()
{
    // Arrange
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Tenant:DefaultTenantId"] = "tenant-a",
            ["Tenants:tenant-a:ConnectionString"] = "Server=srv1;Database=TenantA_DB;",
            ["Tenants:tenant-a:JwtSecret"] = "test-secret-256bits-long-enough"
        })
        .Build();

    var resolver = new TenantResolver(config);

    // Act & Assert
    Assert.Equal("tenant-a", resolver.TenantId);
    Assert.Equal("Server=srv1;Database=TenantA_DB;", resolver.ConnectionString);
    Assert.Equal("test-secret-256bits-long-enough", resolver.JwtSecret);
}

[Fact]
public async Task TenantHeaderHandler_ShouldAdd_TenantIdHeader()
{
    // Arrange
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Tenant:DefaultTenantId"] = "tenant-b"
        })
        .Build();

    var handler = new TenantHeaderHandler(config)
    {
        InnerHandler = new TestHandler()
    };

    var client = new HttpClient(handler);

    // Act
    var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/test");
    await client.SendAsync(request);

    // Assert — verify header was added via TestHandler capture
}

private class TestHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Assert.True(request.Headers.Contains("X-Tenant-Id"));
        Assert.Equal("tenant-b", request.Headers.GetValues("X-Tenant-Id").First());
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }
}
```
