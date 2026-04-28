using MarketMind.Application.Interfaces;
using MarketMind.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketMind.Infrastructure.Jobs;

// Runs every 5 minutes
// Picks up unembedded articles and processes them in batches
// Completely decoupled from the API request
public class ArticleEmbeddingJob(
    INewsRepository              newsRepo,
    IVectorSearchService         vectorSearch,
    IAIOrchestrator              ai,
    ISectorSentimentRepository   sentimentRepo,
    ILogger<ArticleEmbeddingJob> logger)
{
    // Process in small batches — respect Gemini free tier
    // 15 requests/min → batch of 5 with delays = safe
    private const int BatchSize = 5;

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        // Fetch unembedded articles
        var articles = await newsRepo.GetUnembeddedAsync(BatchSize, ct);

        if (articles.Count == 0)
        {
            logger.LogDebug(
                "[EmbeddingJob] No unembedded articles found");
            return;
        }

        logger.LogInformation(
            "[EmbeddingJob] Processing {Count} articles", articles.Count);

        foreach (var article in articles)
        {
            try
            {
                // ── Step 1: Generate embedding ────────────────────
                var embedding = await vectorSearch
                    .EmbedAsync(article.RawText, ct);

                if (embedding.Length > 0)
                {
                    article.SetEmbedding(embedding);
                    await newsRepo.UpdateEmbeddingAsync(article, ct);

                    logger.LogInformation(
                        "[EmbeddingJob] Embedded: {Headline}",
                        article.Headline[..Math.Min(50, article.Headline.Length)]);
                }

                // ── Step 2: Score sentiment ───────────────────────
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
                await newsRepo.UpdateSentimentAsync(article, ct);

                logger.LogInformation(
                    "[EmbeddingJob] Scored {Sentiment} ({Score:P0}) " +
                    "Sectors: {Sectors}",
                    sentiment, score, string.Join(", ", sectors));

                // ── Step 3: Update sector sentiment aggregate ─────
                await UpdateSectorAggregatesAsync(
                    sectors, sentimentType, score, ct);

                // Respect Gemini rate limit — 15 req/min free tier
                // 2 calls per article → max 7 articles/min safely
                await Task.Delay(4000, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "[EmbeddingJob] Failed for article {Id}: {Headline}",
                    article.Id,
                    article.Headline[..Math.Min(50, article.Headline.Length)]);

                // Continue with next article — do not block the batch
            }
        }

        logger.LogInformation(
            "[EmbeddingJob] Batch complete");
    }

    private async Task UpdateSectorAggregatesAsync(
        string[]      sectors,
        SentimentType sentiment,
        float         score,
        CancellationToken ct)
    {
        foreach (var sector in sectors)
        {
            try
            {
                var existing = await sentimentRepo
                    .GetBySectorAsync(sector, DateTime.UtcNow.Date, ct);

                float newScore;
                int   newCount;

                var contribution = sentiment switch
                {
                    SentimentType.Bullish => score * 100,
                    SentimentType.Bearish => (1 - score) * 100,
                    _                     => 50f
                };

                if (existing is not null)
                {
                    newCount = existing.ArticleCount + 1;
                    newScore = ((existing.BullishScore * existing.ArticleCount)
                        + contribution) / newCount;
                }
                else
                {
                    newCount = 1;
                    newScore = contribution;
                }

                var updated = Domain.Entities.SectorSentiment
                    .Create(sector, newScore, newCount);

                await sentimentRepo.SaveAsync(updated, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "[EmbeddingJob] Sector aggregate failed for {Sector}",
                    sector);
            }
        }
    }
}