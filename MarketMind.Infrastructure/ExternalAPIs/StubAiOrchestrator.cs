using MarketMind.Application.DTOs;
using MarketMind.Application.Interfaces;
using MarketMind.Domain.Entities;

namespace MarketMind.Infrastructure.AI;

// Returns realistic fake AI text — replaced by GeminiOrchestrator in Phase 12
public class StubAIOrchestrator : IAIOrchestrator
{
    public Task<(string Text, int TokensUsed)> GeneratePreMarketBriefingAsync(
        List<MarketSnapshotDto> snapshots,
        List<NewsArticle> relevantNews,
        CancellationToken ct = default)
    {
        var giftNifty = snapshots
            .FirstOrDefault(s => s.Symbol == "GIFT_NIFTY");

        var direction = giftNifty?.IsBullish == true ? "gap-up" : "gap-down";
        var pts       = giftNifty?.ChangeAbsolute ?? 142m;

        var text = $"""
            **Global Cues:** Markets are set for a {direction} open today. 
            GIFT NIFTY is trading with a premium of {pts} points, 
            suggesting NIFTY 50 will open around {giftNifty?.Price:N0}. 
            Overnight, the Nasdaq closed +1.82% driven by strong tech earnings 
            and a dovish Federal Reserve commentary. Asian markets are broadly positive.

            **FII & Institutional Flows:** Foreign institutional investors were 
            net buyers at ₹2,458 crore in yesterday's session, primarily in 
            IT and Banking sectors. This sustained buying trend supports the 
            bullish bias heading into today's session. DII activity was muted.

            **Sectors to Watch:** IT sector is the primary focus today given 
            Nasdaq's strong close — TCS and Infosys are likely to lead gains. 
            Banking sector remains supported by stable USD/INR at 83.4 and 
            unchanged RBI repo rate. Crude oil's 2% fall overnight is a 
            tailwind for OMCs — watch BPCL and HPCL for bounce plays.
            """;

        return Task.FromResult((text, 847));
    }

    public Task<(string Text, int TokensUsed)> GenerateSectorAnalysisAsync(
        string sector,
        List<NewsArticle> sectorNews,
        List<TopStockDto> topStocks,
        CancellationToken ct = default)
    {
        var text = $"""
            The {sector} sector is showing significant bullish momentum today, 
            driven by a combination of strong global cues and positive domestic 
            factors. Management commentary from Tier-1 companies suggests a 
            stabilizing deal environment, particularly in BFSI and cloud migration.

            FII flows into {sector} ETFs have been net positive for three 
            consecutive sessions, indicating sustained institutional confidence. 
            The sector's correlation with global peers remains high at 0.85, 
            meaning any continued strength in overseas markets will provide 
            additional tailwind through the session.
            """;

        return Task.FromResult((text, 423));
    }

    public Task<(string Text, int TokensUsed)> GenerateWhyMarketMovedAsync(
        List<OhlcPointDto> ohlc,
        List<NewsArticle> dayNews,
        CancellationToken ct = default)
    {
        var text = $"""
            Today's session saw NIFTY 50 open gap-up at 24,450 before 
            consolidating through the morning. The initial rally was driven 
            by positive global cues from Nasdaq's overnight surge and FII 
            buying in the first 30 minutes.

            The mid-session dip between 11:00 AM and 11:30 AM coincided with 
            crude oil prices recovering from overnight lows, creating mild 
            pressure on OMC stocks and dragging the index lower by 120 points. 
            Support held at the VWAP level of 24,280.

            The afternoon recovery was led by IT heavyweights TCS and Infosys, 
            which attracted fresh FII buying ahead of the US market open. 
            NIFTY closed near day highs, confirming bullish undertone.
            """;

        return Task.FromResult((text, 612));
    }

    public Task<(string Sentiment, float Score, string[] Sectors)>
        ScoreSentimentAsync(
            string headline,
            string text,
            CancellationToken ct = default)
    {
        // Simple keyword-based stub scoring
        var bullishWords = new[] { "surge", "gain", "positive", "buy", "bullish", "rise", "up", "win", "beat" };
        var bearishWords = new[] { "fall", "drop", "negative", "sell", "bearish", "decline", "cut", "miss", "loss" };

        var combined  = $"{headline} {text}".ToLower();
        var bullScore = bullishWords.Count(w => combined.Contains(w));
        var bearScore = bearishWords.Count(w => combined.Contains(w));

        string sentiment;
        float  score;

        if (bullScore > bearScore)
        {
            sentiment = "Bullish";
            score     = Math.Min(0.5f + bullScore * 0.1f, 0.95f);
        }
        else if (bearScore > bullScore)
        {
            sentiment = "Bearish";
            score     = Math.Min(0.5f + bearScore * 0.1f, 0.95f);
        }
        else
        {
            sentiment = "Neutral";
            score     = 0.5f;
        }

        // Stub sector detection
        var sectors = DetectSectors(combined);

        return Task.FromResult((sentiment, score, sectors));
    }

    private static string[] DetectSectors(string text)
    {
        var detected = new List<string>();
        if (text.Contains("it") || text.Contains("tcs") || text.Contains("infosys") || text.Contains("nasdaq"))
            detected.Add("IT");
        if (text.Contains("bank") || text.Contains("rbi") || text.Contains("hdfc") || text.Contains("icici"))
            detected.Add("Banking");
        if (text.Contains("crude") || text.Contains("oil") || text.Contains("bpcl") || text.Contains("ongc"))
            detected.Add("Energy");
        if (text.Contains("gold") || text.Contains("silver"))
            detected.Add("Commodities");
        if (text.Contains("pharma") || text.Contains("drug") || text.Contains("health"))
            detected.Add("Pharma");

        return detected.Count > 0
            ? detected.ToArray()
            : ["General"];
    }
}