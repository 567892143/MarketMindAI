using MarketMind.Domain.Entities;

namespace MarketMind.Application.Interfaces;

// Implemented by NewsRepository in Infrastructure/Persistence
public interface INewsRepository
{
    Task SaveAsync(NewsArticle article, CancellationToken ct = default);
    Task SaveManyAsync(IEnumerable<NewsArticle> articles, CancellationToken ct = default);

    // Returns articles not yet sent to embedding job
    Task<List<NewsArticle>> GetUnembeddedAsync(
        int limit = 50,
        CancellationToken ct = default);

    Task UpdateEmbeddingAsync(NewsArticle article, CancellationToken ct = default);
    Task UpdateSentimentAsync(NewsArticle article, CancellationToken ct = default);

    Task<List<NewsArticle>> GetBySectorAsync(
        string sector,
        DateTime date,
        int limit = 10,
        CancellationToken ct = default);

    Task<List<NewsArticle>> GetByDateAsync(
        DateTime date,
        CancellationToken ct = default);

    // Deduplication — avoid storing same article twice
    Task<bool> ExistsByUrlAsync(string url, CancellationToken ct = default);
}