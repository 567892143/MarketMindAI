using MarketMind.Domain.Enums;

namespace MarketMind.Application.DTOs;

public record NewsArticleDto(
    Guid Id,
    string Headline,
    string Source,
    string Url,
    DateTime PublishedAt,
    SentimentType Sentiment,
    float SentimentScore,
    string[] AffectedSectors
);