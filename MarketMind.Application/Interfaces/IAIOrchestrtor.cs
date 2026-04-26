using MarketMind.Application.DTOs;
using MarketMind.Domain.Entities;

namespace MarketMind.Application.Interfaces;

// Implemented by GeminiOrchestrator in Infrastructure/AI
// Each method = one prompt template + one Gemini call
public interface IAIOrchestrator
{
    // Morning briefing — main feature
    Task<(string Text, int TokensUsed)> GeneratePreMarketBriefingAsync(
        List<MarketSnapshotDto> snapshots,
        List<NewsArticle> relevantNews,
        CancellationToken ct = default);

    // Sector deep dive
    Task<(string Text, int TokensUsed)> GenerateSectorAnalysisAsync(
        string sector,
        List<NewsArticle> sectorNews,
        List<TopStockDto> topStocks,
        CancellationToken ct = default);

    // Post-session Why Engine
    Task<(string Text, int TokensUsed)> GenerateWhyMarketMovedAsync(
        List<OhlcPointDto> ohlc,
        List<NewsArticle> dayNews,
        CancellationToken ct = default);

    // Sentiment scoring for a single article
    Task<(string Sentiment, float Score, string[] Sectors)> ScoreSentimentAsync(
        string headline,
        string text,
        CancellationToken ct = default);
}