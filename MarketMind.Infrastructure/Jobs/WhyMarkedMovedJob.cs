using MarketMind.Application.UseCases.GetWhyMarketMoved;
using Microsoft.Extensions.Logging;

namespace MarketMind.Infrastructure.Jobs;

public class WhyMarketMovedJob(
    GetWhyMarketMovedUseCase    whyUseCase,
    ILogger<WhyMarketMovedJob>  logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        logger.LogInformation(
            "[Hangfire] WhyMarketMovedJob started at {Time}", DateTime.UtcNow);

        try
        {
            var result = await whyUseCase.ExecuteAsync(DateTime.UtcNow, ct);

            logger.LogInformation(
                "[Hangfire] Why Market Moved generated for {Date}",
                result.Date.ToShortDateString());
        }
        catch (InvalidOperationException ex)
        {
            // Market not closed yet — this is fine, job ran too early
            logger.LogWarning(
                "[Hangfire] WhyMarketMovedJob skipped: {Reason}", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Hangfire] WhyMarketMovedJob failed");
            throw;
        }
    }
}