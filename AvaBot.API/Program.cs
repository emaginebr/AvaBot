using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using AvaBot.Application;
using AvaBot.Application.Services;
using AvaBot.API.Auth;
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
    });
builder.Services.AddSingleton<JwtTokenIssuer>();

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

// Elasticsearch - create index on startup
var esService = app.Services.GetRequiredService<IElasticsearchService>();
await esService.CreateIndexAsync();

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

app.UseCors();

// WebSocket
app.UseWebSockets();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapChatWebSocket();

app.Run();
