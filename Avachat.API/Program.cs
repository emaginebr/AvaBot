using Avachat.Application;
using Avachat.API.WebSocket;

var builder = WebApplication.CreateBuilder(args);

// DI
builder.Services.AddAvachatServices(builder.Configuration);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// WebSocket
app.UseWebSockets();

app.UseAuthorization();

app.MapControllers();

app.MapChatWebSocket();

app.Run();
