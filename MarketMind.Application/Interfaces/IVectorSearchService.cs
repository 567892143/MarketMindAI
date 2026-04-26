using MarketMind.Domain.Entities;

namespace MarketMind.Application.Interfaces;

// Implemented by VectorSearchService in Infrastructure/AI
// This is the R in RAG — Retrieval
public interface IVectorSearchService
{
    // Core RAG retrieval — embed query, find k nearest articles
    Task<List<NewsArticle>> SearchAsync(
        string query,
        int k = 5,
        CancellationToken ct = default);

    // Sector-scoped retrieval — only returns articles for that sector
    Task<List<NewsArticle>> SearchBySectorAsync(
        string query,
        string sector,
        int k = 5,
        CancellationToken ct = default);

    // Generate embedding vector for a text — used for articles and queries
    Task<float[]> EmbedAsync(
        string text,
        CancellationToken ct = default);
}