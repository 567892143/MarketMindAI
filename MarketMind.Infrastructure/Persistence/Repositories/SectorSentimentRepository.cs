using MarketMind.Application.Interfaces;
using MarketMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketMind.Infrastructure.Persistence.Repositories;

public class SectorSentimentRepository(AppDbContext db)
    : ISectorSentimentRepository
{
    public async Task SaveAsync(
        SectorSentiment sentiment,
        CancellationToken ct = default)
    {
        // Upsert — replace today's score for this sector
        var existing = await db.SectorSentiments
            .FirstOrDefaultAsync(s =>
                s.Sector == sentiment.Sector &&
                s.Date == sentiment.Date, ct);

        if (existing is not null)
            db.SectorSentiments.Remove(existing);

        db.SectorSentiments.Add(sentiment);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<SectorSentiment>> GetTodayAsync(
        CancellationToken ct = default) =>
        await db.SectorSentiments
            .Where(s => s.Date == DateTime.UtcNow.Date)
            .OrderByDescending(s => s.BullishScore)
            .ToListAsync(ct);

    public async Task<SectorSentiment?> GetBySectorAsync(
        string sector,
        DateTime date,
        CancellationToken ct = default) =>
        await db.SectorSentiments
            .FirstOrDefaultAsync(s =>
                s.Sector == sector &&
                s.Date == date.Date, ct);
}