namespace MarketMind.Application.DTOs;

// Returned for GET /api/v1/sectors/{sector}
// Powers the sector detail screen
public record SectorAnalysisDto(
    string Sector,
    string AnalysisText,
    SectorSentimentDto Sentiment,
    List<NewsArticleDto> DrivingNews,
    List<TopStockDto> TopStocks,
    CrossAssetChainDto CrossAssetChain,
    DateTime GeneratedAt
);

public record TopStockDto(
    string Symbol,
    decimal Price,
    decimal ChangePercent,
    string AiSignal    // "Bullish" / "Bearish" / "Neutral" / "Caution"
);

public record CrossAssetChainDto(
    List<ChainStepDto> Steps,
    string AiInsight
);

public record ChainStepDto(
    string Label,      // "Fed Dovish"
    string Direction,  // "▲" / "▼"
    string Impact      // "Nasdaq ▲ → FII buy IT → IT stocks ▲"
);