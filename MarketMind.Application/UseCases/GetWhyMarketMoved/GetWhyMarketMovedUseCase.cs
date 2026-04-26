using MarketMind.Application.DTOs;
using MarketMind.Application.Interfaces;
using MarketMind.Application.Mappers;
using Microsoft.Extensions.Logging;

namespace MarketMind.Application.UseCases.GetWhyMarketMoved;

public class GetWhyMarketMovedUseCase(
    IAIOrchestrator ai,
    IMarketDataService marketData,
    INewsRepository newsRepo,
    ICacheService cache,
    ILogger<GetWhyMarketMovedUseCase> logger)
{
    public async Task<WhyMarketMovedDto> ExecuteAsync(
        DateTime date,
        CancellationToken ct = default)
    {
        // Default to today if no date passed
        var targetDate = date.Date == default
            ? DateTime.UtcNow.Date
            : date.Date;

        // Only available after market close (3:30 PM IST = 10:00 UTC)
        var marketCloseUtc = targetDate.AddHours(10);
        if (DateTime.UtcNow < marketCloseUtc)
        {
            throw new InvalidOperationException(
                "Why Market Moved analysis is available after 4:00 PM IST.");
        }

        // ── Cache check ───────────────────────────────────────────
        var cacheKey = CacheKeys.WhyMoved(targetDate);
        var cached   = await cache.GetAsync<WhyMarketMovedDto>(cacheKey, ct);
        if (cached is not null) return cached;

        // ── Parallel fetch ────────────────────────────────────────
        var ohlcTask    = marketData.GetIntradayOhlcAsync("NIFTY", targetDate, ct);
        var dayNewsTask = newsRepo.GetByDateAsync(targetDate, ct);

        await Task.WhenAll(ohlcTask, dayNewsTask);

        var ohlc    = await ohlcTask;
        var dayNews = await dayNewsTask;

        // ── Find key price moves ──────────────────────────────────
        // A "key move" = 30-min candle with >0.3% change
        var keyMoves = ohlc
            .Where(o => Math.Abs((double)((o.Close - o.Open) / o.Open * 100)) > 0.3)
            .Select(o => new PriceEventDto(
                Time:             o.Time,
                PriceAt:          o.Close,
                ChangeFromOpen:   o.Close - ohlc.First().Open,
                PossibleCause:    CorrelateWithNews(o.Time, dayNews)
            ))
            .OrderBy(m => m.Time)
            .ToList();

        // ── AI generation ─────────────────────────────────────────
        var (analysisText, tokensUsed) =
            await ai.GenerateWhyMarketMovedAsync(ohlc, dayNews, ct);

        logger.LogInformation(
            "Why Market Moved generated for {Date}. Tokens: {Tokens}",
            targetDate.ToShortDateString(), tokensUsed);

        var result = new WhyMarketMovedDto(
            Date:           targetDate,
            AnalysisText:   analysisText,
            CorrelatedNews: dayNews.Select(MarketMapper.ToDto).ToList(),
            KeyMoves:       keyMoves,
            GeneratedAt:    DateTime.UtcNow
        );

        // Valid for 24 hours — history doesn't change
        await cache.SetAsync(cacheKey, result, TimeSpan.FromHours(24), ct);

        return result;
    }

    // Correlate a price event time with news published within 15 min before it
    private static string CorrelateWithNews(
        DateTime eventTime,
        List<Domain.Entities.NewsArticle> news)
    {
        var correlatedArticle = news
            .Where(n => n.PublishedAt <= eventTime &&
                        n.PublishedAt >= eventTime.AddMinutes(-15))
            .OrderByDescending(n => Math.Abs(n.SentimentScore - 0.5f))
            .FirstOrDefault();

        return correlatedArticle is not null
            ? $"Possible: {correlatedArticle.Headline} ({correlatedArticle.Source})"
            : "No direct news correlation found in 15-min window";
    }
}