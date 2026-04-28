namespace MarketMind.Application.Interfaces;

// Thin abstraction over Hangfire IBackgroundJobClient
// Keeps Application layer free of Hangfire dependency
public interface IJobClient
{
    void TriggerEmbeddingJob();
}