using MarketMind.Application.DTOs;
using MarketMind.Domain.Enums;

namespace MarketMind.Application.Interfaces;

// Implemented by YahooFinanceService in Infrastructure
// One method per concern — Interface Segregation in practice
public interface IMarketDataService
{
    // Single instrument snapshot — NIFTY, CRUDE, USDINR etc.
    Task<MarketSnapshotDto> GetSnapshotAsync(
        string symbol,
        CancellationToken ct = default);

    // All instruments for a module in one call
    Task<List<MarketSnapshotDto>> GetModuleSnapshotsAsync(
        MarketModule module,
        CancellationToken ct = default);

    // Top stocks for a sector — used in sector detail screen
    Task<List<TopStockDto>> GetTopStocksForSectorAsync(
        string sector,
        CancellationToken ct = default);

    // Intraday OHLC — used by Why Engine
    Task<List<OhlcPointDto>> GetIntradayOhlcAsync(
        string symbol,
        DateTime date,
        CancellationToken ct = default);
}

// OHLC used only internally by Why Engine — not in sector/snapshot DTOs
public record OhlcPointDto(
    DateTime Time,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume
);