using MarketMind.Application.DTOs;
using MarketMind.Application.Interfaces;
using MarketMind.Application.Mappers;
using MarketMind.Domain.Entities;
using MarketMind.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketMind.Application.UseCases.GetPreMarketBriefing;

public class GetPreMarketBriefingUseCase(
    IAIOrchestrator ai,
    IVectorSearchService vectorSearch,
    IMarketDataService marketData,
    ICacheService cache,
    ILogger<GetPreMarketBriefingUseCase> logger)
{
    // All symbols the morning briefing needs
    private static readonly string[] BriefingSymbols =
    [
        "GIFT_NIFTY", "NIFTY", "BANKNIFTY",
        "NASDAQ",     "CRUDE", "GOLD", "USDINR"
    ];

    public async Task<PreMarketBriefingDto> ExecuteAsync(
        CancellationToken ct = default)
    {
        // ── Step 1: Cache check ───────────────────────────────────
        // Same briefing served to every user — generated once per morning
        var cached = await cache.GetAsync<PreMarketBriefingDto>(
            CacheKeys.PreMarketBriefing, ct);

        if (cached is not null)
        {
            logger.LogInformation(
                "Pre-market briefing served from cache. Valid until {ValidUntil}",
                cached.ValidUntil);
            return cached;
        }

        logger.LogInformation(
            "Generating fresh pre-market briefing at {Time}", DateTime.UtcNow);

        // ── Step 2: Fetch all market snapshots in parallel ────────
        // Task.WhenAll means all 7 calls happen simultaneously
        // Sequential calls would take 7x longer
        var snapshotTasks = BriefingSymbols
            .Select(symbol => marketData.GetSnapshotAsync(symbol, ct))
            .ToArray();

        var snapshotResults = await Task.WhenAll(snapshotTasks);
        var snapshots = snapshotResults.ToList();

        logger.LogInformation(
            "Fetched {Count} market snapshots", snapshots.Count);

        // ── Step 3: RAG retrieval ─────────────────────────────────
        // Embed the query and find the 5 most relevant news articles
        var relevantNews = await vectorSearch.SearchAsync(
            query: "Indian stock market open global cues FII sentiment",
            k: 5, ct);

        logger.LogInformation(
            "Retrieved {Count} relevant articles via vector search",
            relevantNews.Count);

        // ── Step 4: Generate briefing via Gemini ──────────────────
        var (briefingText, tokensUsed) =
            await ai.GeneratePreMarketBriefingAsync(snapshots, relevantNews, ct);

        // ── Step 5: Build DTO ─────────────────────────────────────
        var generatedAt = DateTime.UtcNow;
        var validUntil  = generatedAt.AddHours(4);

        var result = new PreMarketBriefingDto(
            BriefingText:    briefingText,
            Snapshots:       snapshots,
            SourceArticles:  relevantNews.Select(MarketMapper.ToDto).ToList(),
            SourceNames:     relevantNews.Select(a => a.Source).Distinct().ToArray(),
            IsBullish:       DetermineOverallSentiment(snapshots),
            GeneratedAt:     generatedAt,
            ValidUntil:      validUntil,
            TokensUsed:      tokensUsed
        );

        // ── Step 6: Cache for 4 hours ─────────────────────────────
        await cache.SetAsync(
            CacheKeys.PreMarketBriefing,
            result,
            TimeSpan.FromHours(4),
            ct);

        logger.LogInformation(
            "Pre-market briefing cached. Tokens used: {Tokens}", tokensUsed);

        return result;
    }

    // Overall market direction = majority of key Indian indices
    private static bool DetermineOverallSentiment(
        List<MarketSnapshotDto> snapshots)
    {
        var keyIndices = snapshots
            .Where(s => s.Module == MarketModule.Equities ||
                        s.Symbol == "GIFT_NIFTY")
            .ToList();

        if (keyIndices.Count == 0) return false;

        var bullishCount = keyIndices.Count(s => s.IsBullish);
        return bullishCount > keyIndices.Count / 2;
    }
}