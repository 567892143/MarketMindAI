using MarketMind.Application.DTOs;
using MarketMind.Application.Interfaces;
using MarketMind.Domain.Entities;

namespace MarketMind.Infrastructure.AI;

public static class PromptBuilder
{
    public static string BuildPreMarketPrompt(
        List<MarketSnapshotDto> snapshots,
        List<NewsArticle>       relevantNews)
    {
        var snapshotContext = string.Join("\n", snapshots.Select(s =>
            $"- {s.DisplayName}: {s.Price:N2} " +
            $"{s.DirectionLabel} {s.ChangeAbsolute:+0.##;-0.##} " +
            $"({s.ChangePercent:+0.##;-0.##}%)"));

        var newsContext = string.Join("\n\n", relevantNews.Select((n, i) =>
            $"[Article {i + 1}] {n.Source} — {n.Headline}\n" +
            $"{n.RawText[..Math.Min(300, n.RawText.Length)]}..."));

        return $"""
            You are a senior market analyst for Indian retail traders.
            Generate a pre-market briefing for today.
            Never give explicit buy/sell advice.
            Frame everything as market context and intelligence.
            Use specific numbers from the data provided.
            
            CURRENT MARKET DATA:
            {snapshotContext}
            
            RELEVANT NEWS (last 24 hours):
            {newsContext}
            
            Structure your response in exactly 3 paragraphs:
            1. Global Cues: What happened overnight globally and impact on Indian markets
            2. FII & Institutional Activity: Flow data and institutional sentiment  
            3. Sectors to Watch: Which sectors to focus on today and why
            
            Be direct and useful for a trader starting their day.
            """;
    }

    public static string BuildSectorAnalysisPrompt(
        string            sector,
        List<NewsArticle> sectorNews,
        List<TopStockDto> topStocks)
    {
        var stockContext = string.Join("\n", topStocks.Select(s =>
            $"- {s.Symbol}: {s.Price:N2} ({s.ChangePercent:+0.##;-0.##}%) — {s.AiSignal}"));

        var newsContext = string.Join("\n\n", sectorNews.Select((n, i) =>
            $"[{i + 1}] {n.Source}: {n.Headline}\n" +
            $"{n.RawText[..Math.Min(200, n.RawText.Length)]}..."));

        return $"""
            You are a senior market analyst for Indian retail traders.
            Generate a sector analysis for the {sector} sector for today.
            Never give explicit buy/sell advice.
            
            TOP {sector.ToUpper()} STOCKS:
            {stockContext}
            
            RELEVANT NEWS:
            {newsContext}
            
            Structure your response in exactly 2 paragraphs:
            1. Current momentum and what is driving it (use specific data)
            2. Key risks or catalysts to watch in this session
            
            Be specific, data-driven, and useful for an active trader.
            """;
    }

    public static string BuildWhyMarketMovedPrompt(
        List<OhlcPointDto> ohlc,
        List<NewsArticle>  dayNews)
    {
        var bigMoves = ohlc
            .Where(o => Math.Abs((double)((o.Close - o.Open) / o.Open * 100)) > 0.3)
            .OrderByDescending(o => Math.Abs((double)(o.Close - o.Open)))
            .Take(3)
            .Select(o =>
                $"- {o.Time:HH:mm} UTC: " +
                $"{(o.Close > o.Open ? "▲" : "▼")} " +
                $"{Math.Abs(o.Close - o.Open):N2} pts")
            .ToList();

        var movesContext = bigMoves.Count > 0
            ? string.Join("\n", bigMoves)
            : "No significant intraday moves detected";

        var newsContext = string.Join("\n", dayNews
            .OrderByDescending(n => n.PublishedAt)
            .Take(5)
            .Select(n => $"- {n.PublishedAt:HH:mm} UTC | {n.Source}: {n.Headline}"));

        return $"""
            You are a senior market analyst for Indian retail traders.
            Explain why the Indian stock market (NIFTY 50) moved the way it did today.
            
            KEY INTRADAY PRICE MOVES:
            {movesContext}
            
            NEWS PUBLISHED TODAY:
            {newsContext}
            
            Structure your response in exactly 3 paragraphs:
            1. Opening session — what drove the open direction
            2. Mid-session moves — correlate price moves with news events  
            3. Closing trend — what drove the final hour and tomorrow's outlook
            
            Be specific about timing and causation.
            """;
    }

    public static string BuildSentimentScoringPrompt(
        string headline,
        string text)
    {
        return $$"""
            Analyze the sentiment of this financial news for Indian stock markets.
            
            HEADLINE: {{headline}}
            TEXT: {{text[..Math.Min(500, text.Length)]}}
            
            Respond in exactly this JSON format with no other text:
            {
              "sentiment": "Bullish" or "Bearish" or "Neutral",
              "score": 0.0 to 1.0,
              "sectors": ["sector1", "sector2"]
            }
            
            Sectors must be from: IT, Banking, Energy, Pharma, Auto, FMCG, Metals, Realty, General
            """;
    }
}