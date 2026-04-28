using MarketMind.Application.Interfaces;
using MarketMind.Domain.Entities;
using MarketMind.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using System.Text.Json;

namespace MarketMind.Infrastructure.AI;

// Replaces StubVectorSearchService
// Real RAG retrieval using pgvector cosine similarity
public class VectorSearchService(
    AppDbContext                 db,
    IConfiguration               configuration,
    ILogger<VectorSearchService> logger) : IVectorSearchService
{
    public async Task<List<NewsArticle>> SearchAsync(
        string            query,
        int               k  = 5,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "[RAG] Searching for: '{Query}' (k={K})", query, k);

        var queryVector = await EmbedAsync(query, ct);

        if (queryVector.Length == 0)
        {
            logger.LogWarning(
                "[RAG] Empty embedding — falling back to recent news");
            return await GetRecentAsync(k, ct);
        }

       var vectorLiteral = $"[{string.Join(",", queryVector)}]";
      var cutoffDate    = DateTime.UtcNow.AddDays(-2);

    var results = await db.NewsArticles
        .FromSqlRaw(
            """
            SELECT * FROM news_articles
            WHERE is_embedded = true
              AND embedding IS NOT NULL
              AND published_at >= {0}
            ORDER BY embedding <=> {1}::vector
            LIMIT {2}
            """,
            cutoffDate,
            vectorLiteral,
            k)
        .ToListAsync(ct);

        logger.LogInformation(
            "[RAG] Found {Count} articles via vector search", results.Count);

        // If no embedded articles yet — use recent articles as fallback
        return results.Count > 0
            ? results
            : await GetRecentAsync(k, ct);
    }

    public async Task<List<NewsArticle>> SearchBySectorAsync(
        string            query,
        string            sector,
        int               k  = 5,
        CancellationToken ct = default)
    {
        var queryVector = await EmbedAsync(query, ct);

        if (queryVector.Length == 0)
            return await GetRecentBySectorAsync(sector, k, ct);

        var vectorLiteral = $"[{string.Join(",", queryVector)}]";
        var cutoffDate    = DateTime.UtcNow.AddDays(-2);

        var results = await db.NewsArticles
        .FromSqlRaw(
            """
            SELECT * FROM news_articles
            WHERE is_embedded = true
              AND embedding IS NOT NULL
              AND published_at >= {0}
              AND {1} = ANY(affected_sectors)
            ORDER BY embedding <=> {2}::vector
            LIMIT {3}
            """,
            cutoffDate,
            sector,
            vectorLiteral,
            k)
        .ToListAsync(ct);

        return results.Count > 0
            ? results
            : await GetRecentBySectorAsync(sector, k, ct);
    }

    public async Task<float[]> EmbedAsync(
        string            text,
        CancellationToken ct = default)
    {
        try
        {
            var apiKey = configuration["Gemini:ApiKey"]!;
            var model  = configuration["Gemini:EmbeddingModel"]
                         ?? "text-embedding-004";

            using var http = new HttpClient();
            var url = $"https://generativelanguage.googleapis.com/v1beta/" +
                      $"models/{model}:embedContent?key={apiKey}";

            var truncated = text.Length > 2000 ? text[..2000] : text;

            var body = JsonSerializer.Serialize(new
            {
                model   = $"models/{model}",
                content = new
                {
                    parts = new[] { new { text = truncated } }
                }
            });

            var response = await http.PostAsync(
                url,
                new StringContent(
                    body,
                    System.Text.Encoding.UTF8,
                    "application/json"),
                ct);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var values = doc.RootElement
                .GetProperty("embedding")
                .GetProperty("values")
                .EnumerateArray()
                .Select(v => v.GetSingle())
                .ToArray();

            return values;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[RAG] Embedding failed");
            return [];
        }
    }

    // ── Fallbacks ─────────────────────────────────────────────────

    private async Task<List<NewsArticle>> GetRecentAsync(
        int k, CancellationToken ct) =>
        await db.NewsArticles
            .Where(a => a.PublishedAt >= DateTime.UtcNow.AddDays(-2))
            .OrderByDescending(a => a.PublishedAt)
            .Take(k)
            .ToListAsync(ct);

    private async Task<List<NewsArticle>> GetRecentBySectorAsync(
        string sector, int k, CancellationToken ct) =>
        await db.NewsArticles
            .Where(a => a.AffectedSectors.Contains(sector))
            .OrderByDescending(a => a.PublishedAt)
            .Take(k)
            .ToListAsync(ct);
}