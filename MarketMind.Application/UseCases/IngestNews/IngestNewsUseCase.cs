using MarketMind.Application.Interfaces;
using MarketMind.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketMind.Application.UseCases.IngestNews;

public class IngestNewsUseCase(
    INewsIngestionService newsApi,
    INewsRepository newsRepo,
    IMessagePublisher publisher,
     IJobClient   jobClient,
    ILogger<IngestNewsUseCase> logger)
{
    // Targeted queries — designed to stay within NewsAPI free tier (100/day)
    // 4 queries × 6 runs/day = 24 calls — well within limit
    private static readonly string[] NewsQueries =
    [
        "NIFTY OR SENSEX OR \"Indian stock market\" OR NSE OR BSE",
        "Federal Reserve OR RBI OR \"interest rate\" India market",
        "FII OR \"foreign institutional\" OR \"crude oil\" India",
        "Nasdaq OR \"S&P 500\" OR \"US markets\" impact India"
    ];

    public async Task<IngestNewsResult> ExecuteAsync(
        CancellationToken ct = default)
    {
        var saved    = 0;
        var skipped  = 0;
        var queued   = 0;

        foreach (var query in NewsQueries)
        {
            try
            {
                var rawArticles = await newsApi.FetchLatestAsync(
                    query, pageSize: 20, ct);

                foreach (var raw in rawArticles)
                {
                    // Deduplication — skip if URL already exists
                    if (await newsRepo.ExistsByUrlAsync(raw.Url, ct))
                    {
                        skipped++;
                        continue;
                    }

                    // Create domain entity
                    var article = NewsArticle.Create(
                        raw.Headline,
                        raw.Source,
                        raw.RawText,
                        raw.Url,
                        raw.PublishedAt);

                    await newsRepo.SaveAsync(article, ct);
                    saved++;

                }
            }
            catch (Exception ex)
            {
                // Log and continue — one failed query shouldn't stop others
                logger.LogError(ex,
                    "News ingestion failed for query: {Query}", query);
            }
        }

        logger.LogInformation(
            "News ingestion complete. Saved: {Saved}, Skipped: {Skipped}, Queued: {Queued}",
            saved, skipped, queued);

        if (saved > 0)
        {
            jobClient.TriggerEmbeddingJob();
            logger.LogInformation(
            "[Ingest] Embedding job triggered for {Count} new articles",
            saved);
        }

        return new IngestNewsResult(saved, skipped, queued);
    }
}

public record IngestNewsResult(int Saved, int Skipped, int Queued);