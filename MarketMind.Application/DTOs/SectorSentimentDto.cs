using MarketMind.Domain.Enums;

namespace MarketMind.Application.DTOs;

public record SectorSentimentDto(
    string Sector,
    float BullishScore,
    SentimentType Overall,
    string OverallLabel,
    int ArticleCount,
    DateTime ComputedAt
);