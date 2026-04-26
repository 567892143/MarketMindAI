using MarketMind.Application.DTOs;
using MarketMind.Application.Interfaces;
using MarketMind.Domain.Enums;

namespace MarketMind.Application.UseCases.GetMarketSnapshots;

public class GetMarketSnapshotsUseCase(
    IMarketDataService marketData,
    ICacheService cache)
{
    public async Task<List<MarketSnapshotDto>> ExecuteAsync(
        CancellationToken ct = default)
    {
        var cached = await cache.GetAsync<List<MarketSnapshotDto>>(
            CacheKeys.AllSnapshots, ct);

        if (cached is not null) return cached;

        // Fetch all 5 modules in parallel
        var moduleTasks = Enum.GetValues<MarketModule>()
            .Select(m => marketData.GetModuleSnapshotsAsync(m, ct))
            .ToArray();

        var results = await Task.WhenAll(moduleTasks);
        var all     = results.SelectMany(r => r).ToList();

        // Cache for 1 minute — SignalR handles real-time after this
        await cache.SetAsync(
            CacheKeys.AllSnapshots,
            all,
            TimeSpan.FromMinutes(1),
            ct);

        return all;
    }
}