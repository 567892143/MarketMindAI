namespace MarketMind.Application;

// All Redis cache keys defined in one place
// Use these constants everywhere — never hardcode strings
public static class CacheKeys
{
    public const string PreMarketBriefing  = "analysis:premarket";
    public const string AllSectorSentiment = "sectors:all";
    public const string NiftyFoSnapshot    = "fo:nifty";
    public const string BankNiftyFoSnapshot= "fo:banknifty";
    public const string AllSnapshots       = "snapshots:all";

    // Dynamic keys — built at runtime
    public static string SectorAnalysis(string sector)
        => $"analysis:sector:{sector.ToLower()}";

    public static string WhyMoved(DateTime date)
        => $"analysis:whymoved:{date:yyyy-MM-dd}";

    public static string Snapshot(string symbol)
        => $"snapshot:{symbol.ToLower()}";
}