using MarketMind.Application.Interfaces;
using MarketMind.Domain.Entities;
using MarketMind.Domain.Enums;

namespace MarketMind.Infrastructure.AI;

public class StubVectorSearchService : IVectorSearchService
{
    // Returns pre-built fake articles — real implementation queries pgvector
    private static readonly List<NewsArticle> FakeArticles;

    static StubVectorSearchService()
    {
        FakeArticles =
        [
            CreateArticle(
                "Fed signals dovish stance — rate cuts likely in Q3",
                "Reuters",
                SentimentType.Bullish,
                0.82f,
                ["IT", "Banking"]),

            CreateArticle(
                "FII net buyers at ₹2,458 Cr — IT and Banking lead inflows",
                "Economic Times",
                SentimentType.Bullish,
                0.78f,
                ["IT", "Banking"]),

            CreateArticle(
                "Crude oil falls 2% on surprise inventory build",
                "Bloomberg",
                SentimentType.Bullish,
                0.71f,
                ["Energy", "Commodities"]),

            CreateArticle(
                "TCS wins $400M digital transformation deal",
                "Mint",
                SentimentType.Bullish,
                0.89f,
                ["IT"]),

            CreateArticle(
                "RBI keeps repo rate unchanged at 6.5%",
                "Business Standard",
                SentimentType.Neutral,
                0.55f,
                ["Banking"]),
        ];
    }

    public Task<List<NewsArticle>> SearchAsync(
        string query,
        int k = 5,
        CancellationToken ct = default) =>
        Task.FromResult(FakeArticles.Take(k).ToList());

    public Task<List<NewsArticle>> SearchBySectorAsync(
        string query,
        string sector,
        int k = 5,
        CancellationToken ct = default)
    {
        var filtered = FakeArticles
            .Where(a => a.AffectedSectors
                .Contains(sector, StringComparer.OrdinalIgnoreCase))
            .Take(k)
            .ToList();

        return Task.FromResult(
            filtered.Count > 0 ? filtered : FakeArticles.Take(k).ToList());
    }

    public Task<float[]> EmbedAsync(
        string text,
        CancellationToken ct = default)
    {
        // Return a fake 768-dim vector — all zeros with some variation
        var embedding = new float[768];
        var hash = text.GetHashCode();
        for (int i = 0; i < 768; i++)
            embedding[i] = (float)Math.Sin(hash * (i + 1)) * 0.1f;

        return Task.FromResult(embedding);
    }

    private static NewsArticle CreateArticle(
        string headline,
        string source,
        SentimentType sentiment,
        float score,
        string[] sectors)
    {
        var article = NewsArticle.Create(
            headline, source,
            $"Full text of article: {headline}",
            $"https://{source.ToLower().Replace(" ", "")}.com/{Guid.NewGuid()}",
            DateTime.UtcNow.AddHours(-Random.Shared.Next(1, 12)));

        article.SetSentiment(sentiment, score, sectors);
        return article;
    }
}