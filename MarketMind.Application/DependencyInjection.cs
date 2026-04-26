using MarketMind.Application.UseCases.GetMarketSnapshots;
using MarketMind.Application.UseCases.GetPreMarketBriefing;
using MarketMind.Application.UseCases.GetSectorAnalysis;
using MarketMind.Application.UseCases.GetSectorSentiments;
using MarketMind.Application.UseCases.GetWhyMarketMoved;
using MarketMind.Application.UseCases.IngestNews;
using Microsoft.Extensions.DependencyInjection;

namespace MarketMind.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        // Register all use cases
        services.AddScoped<GetPreMarketBriefingUseCase>();
        services.AddScoped<GetSectorAnalysisUseCase>();
        services.AddScoped<GetWhyMarketMovedUseCase>();
        services.AddScoped<GetMarketSnapshotsUseCase>();
        services.AddScoped<GetSectorSentimentsUseCase>();
        services.AddScoped<IngestNewsUseCase>();

        return services;
    }
}