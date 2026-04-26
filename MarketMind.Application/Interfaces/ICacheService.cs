namespace MarketMind.Application.Interfaces;

// Thin wrapper over IDistributedCache
// Makes use cases testable — no Redis dependency in Application layer
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan expiry,
        CancellationToken ct = default);

    Task RemoveAsync(string key, CancellationToken ct = default);

    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
}