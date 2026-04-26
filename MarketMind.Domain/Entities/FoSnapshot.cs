namespace MarketMind.Domain.Entities;

// F&O data — OI, PCR, Max Pain
// Traders use this to understand institutional positioning
public class FoSnapshot
{
    public Guid Id { get; private set; }
    public string Symbol { get; private set; } = string.Empty; // NIFTY / BANKNIFTY
    public decimal PutCallRatio { get; private set; }
    public decimal MaxPain { get; private set; }
    public List<OiStrike> TopStrikes { get; private set; } = [];
    public DateTime CapturedAt { get; private set; }

    private FoSnapshot() { }

    public static FoSnapshot Create(
        string symbol,
        decimal putCallRatio,
        decimal maxPain,
        List<OiStrike> topStrikes)
    {
        return new FoSnapshot
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            PutCallRatio = putCallRatio,
            MaxPain = maxPain,
            TopStrikes = topStrikes ?? [],
            CapturedAt = DateTime.UtcNow
        };
    }

    // PCR > 1.2 = bullish sentiment, < 0.8 = bearish
    public string PcrSignal => PutCallRatio switch
    {
        >= 1.2m => "Bullish",
        <= 0.8m => "Bearish",
        _ => "Neutral"
    };
}

public class OiStrike
{
    public decimal Strike { get; set; }
    public long CallOI { get; set; }
    public long PutOI { get; set; }

    // Traders interpret high Call OI as resistance, high Put OI as support
    public string Signal => CallOI > PutOI * 1.5m
        ? "Resistance"
        : PutOI > CallOI * 1.5m
            ? "Support"
            : "Neutral";
}