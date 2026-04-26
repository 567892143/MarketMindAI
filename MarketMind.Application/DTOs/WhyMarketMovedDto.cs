namespace MarketMind.Application.DTOs;

// Returned for GET /api/v1/analysis/why-moved
// Post-session Why Engine output
public record WhyMarketMovedDto(
    DateTime Date,
    string AnalysisText,
    List<NewsArticleDto> CorrelatedNews,
    List<PriceEventDto> KeyMoves,
    DateTime GeneratedAt
);

public record PriceEventDto(
    DateTime Time,
    decimal PriceAt,
    decimal ChangeFromOpen,
    string PossibleCause    // AI-generated causal label
);