using Hangfire;
using MarketMind.Application.Interfaces;

namespace MarketMind.Infrastructure.Jobs;

public class HangfireJobClient(
    IBackgroundJobClient client) : IJobClient
{
    public void TriggerEmbeddingJob()
    {
        client.Enqueue<ArticleEmbeddingJob>(
            job => job.ExecuteAsync(CancellationToken.None));
    }
}