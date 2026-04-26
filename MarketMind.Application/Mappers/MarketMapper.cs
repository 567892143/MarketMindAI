using MarketMind.Application.DTOs;
using MarketMind.Domain.Entities;

namespace MarketMind.Application.Mappers;

// Static mappers — pure functions, no DI needed
// Converts Domain entities to DTOs that cross layer boundaries
public static class MarketMapper
{
    public static MarketSnapshotDto ToDto(MarketSnapshot s) => new(
        s.Symbol,
        s.DisplayName,
        s.Module,
        s.Price,
        s.ChangePercent,
        s.ChangeAbsolute,
        s.IsBullish,
        s.DirectionLabel,
        s.CapturedAt
    );

    public static NewsArticleDto ToDto(NewsArticle a) => new(
        a.Id,
        a.Headline,
        a.Source,
        a.Url,
        a.PublishedAt,
        a.Sentiment,
        a.SentimentScore,
        a.AffectedSectors
    );

    public static SectorSentimentDto ToDto(SectorSentiment s) => new(
        s.Sector,
        s.BullishScore,
        s.Overall,
        s.OverallLabel,
        s.ArticleCount,
        s.ComputedAt
    );
}