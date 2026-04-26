using MarketMind.Application.Interfaces;
using MarketMind.Domain.Entities;
using MarketMind.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketMind.Infrastructure.Jobs;

// Aggregates sentiment scores across all sectors
// Runs after news ingestion — refreshes the heatmap data
public class SectorSentimentJob(
    INewsRepository              newsRepo,
    ISectorSentimentRepository   sentimentRepo,
    IAIOrchestrator              ai,
    ILogger<SectorSentimentJob>  logger)
{
    private static readonly string[] TrackedSectors =
    [
        "IT", "Banking", "Energy", "Pharma",
        "Auto", "FMCG", "Metals", "Realty"
    ];

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        logger.LogInformation(
            "[Hangfire] SectorSentimentJob started at {Time}", DateTime.UtcNow);

        var today = DateTime.UtcNow.Date;

        foreach (var sector in TrackedSectors)
        {
            try
            {
                // Get today's articles for this sector
                var articles = await newsRepo.GetBySectorAsync(
                    sector, today, limit: 20, ct);

                if (articles.Count == 0)
                {
                    logger.LogDebug(
                        "[Hangfire] No articles for sector {Sector} today", sector);
                    continue;
                }

                // Score any unscored articles
                float totalScore  = 0;
                int   scoredCount = 0;

                foreach (var article in articles)
                {
                    if (article.Sentiment == SentimentType.Neutral
                        && article.SentimentScore == 0)
                    {
                        // Score via AI
                        var (sentiment, score, sectors) = await ai
                            .ScoreSentimentAsync(
                                article.Headline,
                                article.RawText, ct);

                        var sentimentType = sentiment switch
                        {
                            "Bullish" => SentimentType.Bullish,
                            "Bearish" => SentimentType.Bearish,
                            _         => SentimentType.Neutral
                        };

                        article.SetSentiment(sentimentType, score, sectors);
                        await newsRepo.UpdateSentimentAsync(article);
                    }

                    // Accumulate bullish score
                    var contribution = article.Sentiment switch
                    {
                        SentimentType.Bullish => article.SentimentScore * 100,
                        SentimentType.Bearish => (1 - article.SentimentScore) * 100,
                        _                     => 50f
                    };

                    totalScore += contribution;
                    scoredCount++;
                }

                if (scoredCount == 0) continue;

                // Save aggregated sector sentiment
                var bullishScore = totalScore / scoredCount;
                var sectorSentiment = SectorSentiment.Create(
                    sector, bullishScore, scoredCount);

                await sentimentRepo.SaveAsync(sectorSentiment, ct);

                logger.LogInformation(
                    "[Hangfire] {Sector} sentiment: {Score:F1}% " +
                    "({Count} articles)",
                    sector, bullishScore, scoredCount);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "[Hangfire] Sector sentiment failed for {Sector}", sector);
            }
        }
    }
}