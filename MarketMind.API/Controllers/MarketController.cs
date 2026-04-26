using MarketMind.Application.DTOs;
using MarketMind.Application.UseCases.GetMarketSnapshots;
using MarketMind.Application.UseCases.GetPreMarketBriefing;
using Microsoft.AspNetCore.Mvc;

namespace MarketMind.API.Controllers;

[ApiController]
[Route("api/v1/market")]
[Produces("application/json")]
public class MarketController(
    GetPreMarketBriefingUseCase briefingUseCase,
    GetMarketSnapshotsUseCase   snapshotsUseCase) : ControllerBase
{
    /// <summary>
    /// Get today's AI-generated pre-market briefing.
    /// Served from cache after first generation at 7:45 AM IST.
    /// </summary>
    [HttpGet("briefing")]
    [ProducesResponseType(typeof(ApiResponse<PreMarketBriefingDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GetBriefing(CancellationToken ct)
    {
        var result = await briefingUseCase.ExecuteAsync(ct);
        return Ok(ApiResponse<PreMarketBriefingDto>.Ok(result));
    }

    /// <summary>
    /// Get live snapshots for all instruments across all 5 market modules.
    /// </summary>
    [HttpGet("snapshots")]
    [ProducesResponseType(typeof(ApiResponse<List<MarketSnapshotDto>>), 200)]
    public async Task<IActionResult> GetSnapshots(CancellationToken ct)
    {
        var result = await snapshotsUseCase.ExecuteAsync(ct);
        return Ok(ApiResponse<List<MarketSnapshotDto>>.Ok(result));
    }

    /// <summary>
    /// Get snapshot for a single instrument.
    /// Symbols: NIFTY, SENSEX, BANKNIFTY, GIFT_NIFTY, CRUDE, GOLD, USDINR, NASDAQ
    /// </summary>
    [HttpGet("snapshots/{symbol}")]
    [ProducesResponseType(typeof(ApiResponse<MarketSnapshotDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetSnapshot(
        string symbol,
        CancellationToken ct)
    {
        var result = await snapshotsUseCase.ExecuteAsync(ct);
        var snap   = result.FirstOrDefault(s =>
            s.Symbol.Equals(symbol.ToUpper(), StringComparison.OrdinalIgnoreCase));

        if (snap is null)
            return NotFound(ApiResponse<object>.Fail(
                $"Symbol '{symbol}' not found."));

        return Ok(ApiResponse<MarketSnapshotDto>.Ok(snap));
    }
}