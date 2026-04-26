using MarketMind.Application.DTOs;

namespace MarketMind.Application.Interfaces;

// Implemented by NseDataService in Infrastructure/ExternalAPIs
// NSE provides F&O OI data for free
public interface IFoDataService
{
    Task<FoSnapshotDto> GetNiftyFoAsync(CancellationToken ct = default);
    Task<FoSnapshotDto> GetBankNiftyFoAsync(CancellationToken ct = default);
}