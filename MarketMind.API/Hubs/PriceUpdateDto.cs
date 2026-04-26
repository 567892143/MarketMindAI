namespace MarketMind.API.Hubs;

// These are the payloads pushed to Angular via SignalR
// Keep them lean — only what the UI needs to update

public record PriceUpdateDto(
    string  Symbol,
    string  DisplayName,
    decimal Price,
    decimal ChangePercent,
    decimal ChangeAbsolute,
    bool    IsBullish,
    string  DirectionLabel,
    DateTime UpdatedAt
);

public record BriefingReadyDto(
    string  Message,
    DateTime GeneratedAt,
    bool    IsBullish
);

public record MarketStatusDto(
    bool    IsMarketOpen,
    string  Session,        // "Pre-Market" | "Market" | "Post-Market" | "Closed"
    DateTime NextEvent,     // Next open or close time
    string  NextEventLabel  // "Market opens in" | "Market closes in"
);