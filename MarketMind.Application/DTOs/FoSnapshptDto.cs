namespace MarketMind.Application.DTOs;

public record FoSnapshotDto(
    string Symbol,
    decimal PutCallRatio,
    string PcrSignal,
    decimal MaxPain,
    List<OiStrikeDto> TopStrikes,
    DateTime CapturedAt
);

public record OiStrikeDto(
    decimal Strike,
    long CallOI,
    long PutOI,
    string Signal
);