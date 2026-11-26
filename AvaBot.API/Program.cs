using AvaBot.Infra.Interfaces.Repository;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using AvaBot.Infra.Context;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ModelContextProtocol.Protocol;
using AvaBot.Application;
using AvaBot.Application.Services;
using AvaBot.API.Auth;
using AvaBot.API.Database;
using AvaBot.API.Mcp;
using AvaBot.API.Mcp.Tools;
using AvaBot.API.WebSocket;
using AvaBot.Infra.Interfaces.AppServices;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Logging — troca o provider de console padrao (sem timestamp) por um com data/hora em cada linha
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
    options.SingleLine = true;
});
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

// DI
builder.Services.AddAvaBotServices(builder.Configuration);

// Controllers
builder.Services.AddControllers();

// Validators: consumidos explicitamente pelos controllers (sem auto-validacao global)
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT token. Example: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Authentication
var jwtSecret = builder.Configuration["Auth:JwtSecret"]
    ?? throw new InvalidOperationException("Auth:JwtSecret is required");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
        // O "sub" do token vira ClaimTypes.NameIdentifier (mapeamento padrao); User.GetUserId() le os dois.
        options.MapInboundClaims = true;
    })
    // Chave de acesso dos assistentes (feature 017): so vale em /mcp; o JWT do painel nao vale la.
    .AddScheme<AuthenticationSchemeOptions, McpAccessKeyAuthenticationHandler>(McpAccessKeyAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(McpAccessKeyAuthenticationHandler.PolicyName, policy =>
        policy.AddAuthenticationSchemes(McpAccessKeyAuthenticationHandler.SchemeName).RequireAuthenticatedUser());
});
builder.Services.AddSingleton<JwtTokenIssuer>();

// Servidor MCP (feature 017): Streamable HTTP sem estado em /mcp; uma classe de ferramentas por grupo.
builder.Services.AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation
        {
            Name = "avabot",
            Title = "AvaBot",
            Version = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "1.0.0"
        };
        options.ServerInstructions = McpServerInstructions.Text;
    })
    .WithHttpTransport()
    .WithTools<AccountTools>(McpJson.Options)
    .WithTools<AgentTools>(McpJson.Options)
    .WithTools<ConversationTools>(McpJson.Options)
    .WithTools<PowerBITools>(McpJson.Options)
    .WithTools<ChannelTools>(McpJson.Options)
    .WithTools<WhatsappAccessTools>(McpJson.Options)
    .WithTools<KnowledgeTools>(McpJson.Options)
    .WithRequestFilters(filters =>
    {
        filters.AddCallToolFilter(McpSecretGuardFilter.Create());
        filters.AddCallToolFilter(McpToolExecutionFilter.Create());
    })
    .AddAuthorizationFilters();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment() || builder.Environment.EnvironmentName == "Docker")
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// Banco de dados: aplica as migracoes EF pendentes antes de qualquer outro acesso (a partir de
// AddUsersAndAgentOwner). Bancos anteriores precisam de scripts/seed-ef-migrations-history.sql
// uma vez; sem ele a API nao sobe e o erro explica. Desliga-se com Database:MigrateOnStartup=false.
await StartupMigrator.MigrateAsync(app.Services, app.Configuration);


// Contas de usuario (feature 016): cria a conta do administrador na primeira subida e
// atribui os agentes existentes. Lanca (e a API nao sobe) se restar agente sem dono.
using (var scope = app.Services.CreateScope())
{
    var bootstrap = scope.ServiceProvider.GetRequiredService<UserBootstrapService>();
    await bootstrap.EnsureAdminAccountAsync();
}

// Swagger
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Atras do ingress-nginx e da Cloudflare: o Service e ClusterIP (so o ingress fala com a API),
// entao a API confia em X-Forwarded-For/Proto para ter o IP real do cliente e o esquema https.
var forwardedHeaders = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
forwardedHeaders.KnownNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaders);

// No Kubernetes o ingress encaminha /api/* sem reescrever o caminho: PathBase=/api faz a API
// aceitar /api/agents (publico) e continuar aceitando /agents (chamadas internas).
var pathBase = app.Configuration["PathBase"];
if (!string.IsNullOrWhiteSpace(pathBase))
{
    app.UsePathBase(pathBase);
}

app.UseCors();

// WebSocket
app.UseWebSockets();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapChatWebSocket();

// Readiness do Kubernetes: 200 so quando o banco responde.
app.MapGet("/health", async (AvaBotContext db, CancellationToken ct) =>
        await db.Database.CanConnectAsync(ct)
            ? Results.Ok(new { status = "ok" })
            : Results.Json(new { status = "database_unavailable" }, statusCode: StatusCodes.Status503ServiceUnavailable))
    .AllowAnonymous();

app.MapMcp("/mcp").RequireAuthorization(McpAccessKeyAuthenticationHandler.PolicyName);

app.Run();
