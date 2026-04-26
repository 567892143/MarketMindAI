using MarketMind.Domain.Enums;

namespace MarketMind.Domain.Entities;

public class NewsArticle
{
    public Guid Id { get; private set; }
    public string Headline { get; private set; } = string.Empty;
    public string Source { get; private set; } = string.Empty;
    public string RawText { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public DateTime PublishedAt { get; private set; }
    public SentimentType Sentiment { get; private set; }
    public float SentimentScore { get; private set; }
    public string[] AffectedSectors { get; private set; } = [];
    public float[]? Embedding { get; private set; }
    public bool IsEmbedded { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core needs this
    private NewsArticle() { }

    public static NewsArticle Create(
        string headline,
        string source,
        string rawText,
        string url,
        DateTime publishedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(headline);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        return new NewsArticle
        {
            Id = Guid.NewGuid(),
            Headline = headline,
            Source = source,
            RawText = rawText,
            Url = url,
            PublishedAt = publishedAt,
            Sentiment = SentimentType.Neutral,
            SentimentScore = 0f,
            AffectedSectors = [],
            IsEmbedded = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Domain method — encapsulates mutation, keeps entity in control
    public void SetEmbedding(float[] embedding)
    {
        ArgumentNullException.ThrowIfNull(embedding);
        Embedding = embedding;
        IsEmbedded = true;
    }

    public void SetSentiment(
        SentimentType sentiment,
        float score,
        string[] affectedSectors)
    {
        Sentiment = sentiment;
        SentimentScore = Math.Clamp(score, 0f, 1f);
        AffectedSectors = affectedSectors ?? [];
    }
}