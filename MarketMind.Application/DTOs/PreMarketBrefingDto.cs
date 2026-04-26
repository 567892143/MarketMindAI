namespace MarketMind.Application.DTOs;

// This is what the API returns for GET /api/v1/market/briefing
// Everything the Angular dashboard needs in one response
public record PreMarketBriefingDto(
    string BriefingText,
    List<MarketSnapshotDto> Snapshots,
    List<NewsArticleDto> SourceArticles,
    string[] SourceNames,          // ["Reuters", "ET", "Bloomberg"]
    bool IsBullish,
    DateTime GeneratedAt,
    DateTime ValidUntil,
    int TokensUsed
);