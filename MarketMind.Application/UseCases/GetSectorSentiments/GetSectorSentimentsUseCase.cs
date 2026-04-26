using MarketMind.Application.DTOs;
using MarketMind.Application.Interfaces;
using MarketMind.Application.Mappers;

namespace MarketMind.Application.UseCases.GetSectorSentiments;

public class GetSectorSentimentsUseCase(
    ISectorSentimentRepository sentimentRepo,
    ICacheService cache)
{
    public async Task<List<SectorSentimentDto>> ExecuteAsync(
        CancellationToken ct = default)
    {
        var cached = await cache.GetAsync<List<SectorSentimentDto>>(
            CacheKeys.AllSectorSentiment, ct);

        if (cached is not null) return cached;

        var sentiments = await sentimentRepo.GetTodayAsync(ct);
        var result     = sentiments.Select(MarketMapper.ToDto).ToList();

        // Cache for 30 min — sector scores update every 30 min with news ingestion
        await cache.SetAsync(
            CacheKeys.AllSectorSentiment,
            result,
            TimeSpan.FromMinutes(30),
            ct);

        return result;
    }
}