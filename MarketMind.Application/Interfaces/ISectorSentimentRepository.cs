using MarketMind.Domain.Entities;

namespace MarketMind.Application.Interfaces;

// Implemented by SectorSentimentRepository in Infrastructure/Persistence
public interface ISectorSentimentRepository
{
    Task SaveAsync(SectorSentiment sentiment, CancellationToken ct = default);
    Task<List<SectorSentiment>> GetTodayAsync(CancellationToken ct = default);
    Task<SectorSentiment?> GetBySectorAsync(
        string sector,
        DateTime date,
        CancellationToken ct = default);
}