using MarketMind.Application.UseCases.GetPreMarketBriefing;
using Microsoft.Extensions.Logging;

namespace MarketMind.Infrastructure.Jobs;

public class MorningBriefingJob(
    GetPreMarketBriefingUseCase briefingUseCase,
    ILogger<MorningBriefingJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        logger.LogInformation(
            "[Hangfire] MorningBriefingJob started at {Time}", DateTime.UtcNow);

        try
        {
            // Force fresh generation — clears cache first
            await briefingUseCase.ExecuteAsync(ct);

            logger.LogInformation(
                "[Hangfire] Morning briefing generated and cached successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Hangfire] MorningBriefingJob failed");
            throw;
        }
    }
}