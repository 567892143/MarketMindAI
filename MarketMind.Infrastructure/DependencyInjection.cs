using MarketMind.Application.Interfaces;
using MarketMind.Infrastructure.AI;
using MarketMind.Infrastructure.ExternalAPIs;
using MarketMind.Infrastructure.Messaging;
using MarketMind.Infrastructure.Persistence;
using MarketMind.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MarketMind.Infrastructure;

// Extension method pattern — keeps Program.cs clean
// All infrastructure wiring happens here
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database ──────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                o => o.UseVector())
            .UseSnakeCaseNamingConvention());

        // ── Repositories ──────────────────────────────────────────
        services.AddScoped<INewsRepository,           NewsRepository>();
        services.AddScoped<ISectorSentimentRepository,SectorSentimentRepository>();

        // ── Cache ─────────────────────────────────────────────────
        services.AddStackExchangeRedisCache(options =>
            options.Configuration =
                configuration["Redis:ConnectionString"]);
        services.AddScoped<ICacheService, RedisCacheService>();

        // ── Stub implementations (replaced phase by phase) ────────
        services.AddScoped<IMarketDataService,   StubMarketDataService>();
        services.AddScoped<INewsIngestionService, StubNewsIngestionService>();
        services.AddScoped<IAIOrchestrator,       StubAIOrchestrator>();
        services.AddScoped<IVectorSearchService,  StubVectorSearchService>();
        services.AddScoped<IMessagePublisher,     StubMessagePublisher>();
        services.AddScoped<IFoDataService,        StubFoDataService>();

        return services;
    }
}