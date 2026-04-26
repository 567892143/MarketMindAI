using MarketMind.Domain.Enums;

namespace MarketMind.Domain.Entities;

public class MarketSnapshot
{
    public Guid Id { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public MarketModule Module { get; private set; }
    public decimal Price { get; private set; }
    public decimal ChangePercent { get; private set; }
    public decimal ChangeAbsolute { get; private set; }
    public decimal? Volume { get; private set; }
    public decimal? DayHigh { get; private set; }
    public decimal? DayLow { get; private set; }
    public DateTime CapturedAt { get; private set; }

    private MarketSnapshot() { }

    public static MarketSnapshot Create(
        string symbol,
        string displayName,
        MarketModule module,
        decimal price,
        decimal changePercent,
        decimal changeAbsolute)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        return new MarketSnapshot
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            DisplayName = displayName,
            Module = module,
            Price = price,
            ChangePercent = changePercent,
            ChangeAbsolute = changeAbsolute,
            CapturedAt = DateTime.UtcNow
        };
    }

    public void EnrichWithDayData(
        decimal volume,
        decimal dayHigh,
        decimal dayLow)
    {
        Volume = volume;
        DayHigh = dayHigh;
        DayLow = dayLow;
    }

    // Convenience — traders read direction constantly
    public bool IsBullish => ChangePercent > 0;
    public bool IsBearish => ChangePercent < 0;
    public string DirectionLabel => ChangePercent switch
    {
        > 0 => "▲",
        < 0 => "▼",
        _ => "–"
    };
}