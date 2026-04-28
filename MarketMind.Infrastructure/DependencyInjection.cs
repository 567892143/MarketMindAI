using MarketMind.Application.Interfaces;
using MarketMind.Infrastructure.AI;
using MarketMind.Infrastructure.ExternalAPIs;
using MarketMind.Infrastructure.Messaging;
using MarketMind.Infrastructure.Persistence;
using MarketMind.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Hangfire;
using Hangfire.PostgreSql;
using MarketMind.Infrastructure.Jobs;

namespace MarketMind.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database ──────────────────────────────────────────────
        NpgsqlConnection.GlobalTypeMapper.UseVector();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                o => o.UseVector())
            .UseSnakeCaseNamingConvention());

        services.AddHangfire(config => config
.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
.UseSimpleAssemblyNameTypeSerializer()
.UseRecommendedSerializerSettings()
.UsePostgreSqlStorage(options =>
    options.UseNpgsqlConnection(
        configuration.GetConnectionString("DefaultConnection"))));

        services.AddHangfireServer(options =>
        {
            // Worker count — how many jobs run simultaneously
            options.WorkerCount = 2;

            // Which queues this server processes
            options.Queues = ["default", "critical"];
        });


        // ── Repositories ──────────────────────────────────────────
        services.AddScoped<INewsRepository, NewsRepository>();
        services.AddScoped<ISectorSentimentRepository, SectorSentimentRepository>();

        // ── Cache ─────────────────────────────────────────────────
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration["Redis:ConnectionString"];
            options.InstanceName = "marketmind:";
        });
        services.AddScoped<ICacheService, RedisCacheService>();

        // ── HTTP Clients with Polly retry + circuit breaker ───────
        // Yahoo Finance — real market data
        services.AddHttpClient<IMarketDataService, YahooFinanceService>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddPolicyHandler(HttpClientPolicies.GetRetryPolicy())
        .AddPolicyHandler(HttpClientPolicies.GetCircuitBreakerPolicy());

        // NewsAPI — real news ingestion
        services.AddHttpClient<INewsIngestionService, NewsAPIService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Add("User-Agent", "MarketMindAI/1.0");
        })
        .AddPolicyHandler(HttpClientPolicies.GetRetryPolicy())
        .AddPolicyHandler(HttpClientPolicies.GetCircuitBreakerPolicy());



        // ── Still stubbed — replaced in later phases ───────────────
        // services.AddScoped<IAIOrchestrator, StubAIOrchestrator>();
        // services.AddScoped<IVectorSearchService, StubVectorSearchService>();
        services.AddScoped<IMessagePublisher, StubMessagePublisher>();
        services.AddScoped<IFoDataService, StubFoDataService>();

        services.AddScoped<IAIOrchestrator,      GeminiOrchestrator>();
        services.AddScoped<IVectorSearchService, VectorSearchService>();
        services.AddScoped<IJobClient, HangfireJobClient>();

        services.AddScoped<MorningBriefingJob>();
        services.AddScoped<NewsIngestionJob>();
        services.AddScoped<WhyMarketMovedJob>();
        services.AddScoped<SectorSentimentJob>();
        services.AddScoped<ArticleEmbeddingJob>();

        return services;
    }
}