using MarketMind.API.Hubs;
using MarketMind.API.Middleware;
using MarketMind.Application;
using MarketMind.Infrastructure;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ── Services ───────────────────────────────────────────────────────
builder.Services.AddControllers();

// Clean Architecture layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var azureSignalRConnection = builder.Configuration["AzureSignalR:ConnectionString"];

if (!string.IsNullOrWhiteSpace(azureSignalRConnection) &&
    !azureSignalRConnection.Contains("placeholder"))
{
    // Production — Azure SignalR Service
    builder.Services.AddSignalR()
        .AddAzureSignalR(azureSignalRConnection);
}
else
{
    // Development — local SignalR (no Azure needed)
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = true; // helpful during development
        options.KeepAliveInterval    = TimeSpan.FromSeconds(15);
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    });
}

builder.Services.AddHostedService<MarketPriceBackgroundService>();
builder.Services.AddHostedService<BriefingNotificationService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "MarketMind AI API",
        Version     = "v1",
        Description = "AI-powered stock market intelligence for Indian traders"
    });
    c.EnableAnnotations();
});

// CORS — allows Angular dev server to call the API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins(
                "http://localhost:4200",  // Angular dev server
                "http://localhost:4201")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());         // Required for SignalR
});

// ── Pipeline ───────────────────────────────────────────────────────
var app = builder.Build();

// Global exception handler — first in pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MarketMind AI v1");
        c.RoutePrefix = string.Empty; // Swagger at root URL
    });
}

app.UseCors("AllowAngular");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapHub<MarketHub>("/hubs/market");

app.Run();