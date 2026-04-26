using MarketMind.Domain.Enums;

namespace MarketMind.Domain.Entities;

public class SectorSentiment
{
    public Guid Id { get; private set; }
    public string Sector { get; private set; } = string.Empty;
    public float BullishScore { get; private set; }   // 0 to 100
    public SentimentType Overall { get; private set; }
    public int ArticleCount { get; private set; }
    public DateTime ComputedAt { get; private set; }
    public DateTime Date { get; private set; }

    private SectorSentiment() { }

    public static SectorSentiment Create(
        string sector,
        float bullishScore,
        int articleCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sector);

        return new SectorSentiment
        {
            Id = Guid.NewGuid(),
            Sector = sector,
            BullishScore = Math.Clamp(bullishScore, 0f, 100f),
            Overall = bullishScore switch
            {
                >= 60 => SentimentType.Bullish,
                <= 40 => SentimentType.Bearish,
                _ => SentimentType.Neutral
            },
            ArticleCount = articleCount,
            ComputedAt = DateTime.UtcNow,
            Date = DateTime.UtcNow.Date
        };
    }

    // Read-only label for API responses
    public string OverallLabel => Overall switch
    {
        SentimentType.Bullish => "Bullish",
        SentimentType.Bearish => "Bearish",
        _ => "Neutral"
    };
}