using MarketMind.Application.DTOs;
using MarketMind.Application.Interfaces;
using MarketMind.Application.Mappers;
using Microsoft.Extensions.Logging;

namespace MarketMind.Application.UseCases.GetSectorAnalysis;

public class GetSectorAnalysisUseCase(
    IAIOrchestrator ai,
    IVectorSearchService vectorSearch,
    IMarketDataService marketData,
    ISectorSentimentRepository sentimentRepo,
    ICacheService cache,
    ILogger<GetSectorAnalysisUseCase> logger)
{
    public async Task<SectorAnalysisDto> ExecuteAsync(
        string sector,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sector);

        // ── Step 1: Cache check ───────────────────────────────────
        var cacheKey = CacheKeys.SectorAnalysis(sector);
        var cached   = await cache.GetAsync<SectorAnalysisDto>(cacheKey, ct);

        if (cached is not null)
        {
            logger.LogInformation(
                "Sector analysis for {Sector} served from cache", sector);
            return cached;
        }

        // ── Step 2: Parallel data fetch ───────────────────────────
        var newsTask      = vectorSearch.SearchBySectorAsync(
                                $"{sector} sector India stocks market",
                                sector, k: 5, ct);
        var topStocksTask = marketData.GetTopStocksForSectorAsync(sector, ct);
        var sentimentTask = sentimentRepo.GetBySectorAsync(
                                sector, DateTime.UtcNow.Date, ct);

        await Task.WhenAll(newsTask, topStocksTask, sentimentTask);

        var sectorNews = await newsTask;
        var topStocks  = await topStocksTask;
        var sentiment  = await sentimentTask;

        // ── Step 3: AI analysis ───────────────────────────────────
        var (analysisText, tokensUsed) =
            await ai.GenerateSectorAnalysisAsync(sector, sectorNews, topStocks, ct);

        logger.LogInformation(
            "Sector analysis for {Sector} generated. Tokens: {Tokens}",
            sector, tokensUsed);

        // ── Step 4: Build cross-asset chain ───────────────────────
        // Explains the causal chain driving this sector today
        var chain = BuildCrossAssetChain(sector, topStocks);

        // ── Step 5: Assemble DTO ──────────────────────────────────
        var sentimentDto = sentiment is not null
            ? MarketMapper.ToDto(sentiment)
            : new SectorSentimentDto(sector, 50f,
                Domain.Enums.SentimentType.Neutral,
                "Neutral", 0, DateTime.UtcNow);

        var result = new SectorAnalysisDto(
            Sector:         sector,
            AnalysisText:   analysisText,
            Sentiment:      sentimentDto,
            DrivingNews:    sectorNews.Select(MarketMapper.ToDto).ToList(),
            TopStocks:      topStocks,
            CrossAssetChain: chain,
            GeneratedAt:    DateTime.UtcNow
        );

        // ── Step 6: Cache for 2 hours ─────────────────────────────
        await cache.SetAsync(cacheKey, result, TimeSpan.FromHours(2), ct);

        return result;
    }

    // Cross-asset chain is rule-based — no AI needed
    // Crude → INR → OMCs | Nasdaq → FII → IT | Fed → Bonds → Banks
    private static CrossAssetChainDto BuildCrossAssetChain(
        string sector,
        List<TopStockDto> topStocks)
    {
        var isPositive = topStocks.Count(s =>
            s.ChangePercent > 0) > topStocks.Count / 2;

        var (steps, insight) = sector.ToUpper() switch
        {
            "IT" or "IT SERVICES" => (
                new List<ChainStepDto>
                {
                    new("Fed Dovish",  "▲", "US rate expectations ease"),
                    new("Nasdaq",      "▲", "Tech sentiment improves"),
                    new("FII Buy IT",  "▲", "Foreign inflows into IT ETFs"),
                    new("IT Stocks",   isPositive ? "▲" : "▼", "Indian IT follows")
                },
                "Nasdaq direction is the strongest single predictor for Indian IT."
            ),
            "BANKING" or "FINANCIALS" => (
                new List<ChainStepDto>
                {
                    new("RBI Policy",   "▲", "Rate decision shapes NIM outlook"),
                    new("Bond Yields",  "▼", "Lower yields = better margins"),
                    new("FII Flows",    "▲", "Banks attract FII on rate clarity"),
                    new("Bank Stocks",  isPositive ? "▲" : "▼", "HDFC, ICICI lead")
                },
                "RBI rate stance and bond yield direction are the primary drivers."
            ),
            "ENERGY" or "OIL AND GAS" => (
                new List<ChainStepDto>
                {
                    new("Crude Oil",   isPositive ? "▲" : "▼", "Global supply/demand"),
                    new("USD/INR",     "▲", "Import cost pressure on OMCs"),
                    new("OMC Margins", "▼", "HPCL, BPCL under pressure"),
                    new("Energy",      isPositive ? "▲" : "▼", "Upstream vs downstream split")
                },
                "Crude price is the direct driver — every $1 move impacts OMC margins."
            ),
            _ => (
                new List<ChainStepDto>
                {
                    new("Global Cues",  "▲", "Overnight sentiment"),
                    new("FII Flows",    "▲", "Foreign institutional direction"),
                    new("Sector",       isPositive ? "▲" : "▼", $"{sector} follows")
                },
                $"Global cues and FII flow direction are primary drivers for {sector}."
            )
        };

        return new CrossAssetChainDto(steps, insight);
    }
}