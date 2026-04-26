using MarketMind.Application.DTOs;
using MarketMind.Application.Interfaces;

namespace MarketMind.Infrastructure.ExternalAPIs;

public class StubFoDataService : IFoDataService
{
    public Task<FoSnapshotDto> GetNiftyFoAsync(
        CancellationToken ct = default) =>
        Task.FromResult(new FoSnapshotDto(
            Symbol:      "NIFTY",
            PutCallRatio: 1.24m,
            PcrSignal:   "Bullish",
            MaxPain:     24200m,
            TopStrikes:
            [
                new(24000m, 8_200_000L, 12_400_000L, "Support"),
                new(24500m, 14_100_000L, 6_800_000L, "Resistance"),
                new(25000m, 18_300_000L, 3_200_000L, "Strong Resistance"),
            ],
            CapturedAt: DateTime.UtcNow
        ));

    public Task<FoSnapshotDto> GetBankNiftyFoAsync(
        CancellationToken ct = default) =>
        Task.FromResult(new FoSnapshotDto(
            Symbol:      "BANKNIFTY",
            PutCallRatio: 0.98m,
            PcrSignal:   "Neutral",
            MaxPain:     52000m,
            TopStrikes:
            [
                new(51000m, 6_100_000L, 9_200_000L, "Support"),
                new(52000m, 11_400_000L, 7_100_000L, "Neutral"),
                new(53000m, 14_800_000L, 2_900_000L, "Resistance"),
            ],
            CapturedAt: DateTime.UtcNow
        ));
}