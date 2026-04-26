using MarketMind.Domain.Enums;

namespace MarketMind.Application.DTOs;

public record MarketSnapshotDto(
    string Symbol,
    string DisplayName,
    MarketModule Module,
    decimal Price,
    decimal ChangePercent,
    decimal ChangeAbsolute,
    bool IsBullish,
    string DirectionLabel,
    DateTime CapturedAt
);