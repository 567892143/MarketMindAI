using MarketMind.Application.UseCases.IngestNews;
using Microsoft.Extensions.Logging;

namespace MarketMind.Infrastructure.Jobs;

public class NewsIngestionJob(
    IngestNewsUseCase           ingestUseCase,
    ILogger<NewsIngestionJob>   logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        logger.LogInformation(
            "[Hangfire] NewsIngestionJob started at {Time}", DateTime.UtcNow);

        try
        {
            var result = await ingestUseCase.ExecuteAsync(ct);

            logger.LogInformation(
                "[Hangfire] News ingestion complete — " +
                "Saved: {Saved}, Skipped: {Skipped}, Queued: {Queued}",
                result.Saved, result.Skipped, result.Queued);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Hangfire] NewsIngestionJob failed");
            throw;
        }
    }
}