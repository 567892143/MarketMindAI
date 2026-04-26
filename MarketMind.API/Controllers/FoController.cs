using MarketMind.Application.DTOs;
using MarketMind.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarketMind.API.Controllers;

[ApiController]
[Route("api/v1/fo")]
[Produces("application/json")]
public class FoController(IFoDataService foService) : ControllerBase
{
    /// <summary>
    /// Get F&O snapshot for NIFTY — PCR, Max Pain, top OI strikes.
    /// </summary>
    [HttpGet("nifty")]
    [ProducesResponseType(typeof(ApiResponse<FoSnapshotDto>), 200)]
    public async Task<IActionResult> GetNiftyFo(CancellationToken ct)
    {
        var result = await foService.GetNiftyFoAsync(ct);
        return Ok(ApiResponse<FoSnapshotDto>.Ok(result));
    }

    /// <summary>
    /// Get F&O snapshot for BANK NIFTY.
    /// </summary>
    [HttpGet("banknifty")]
    [ProducesResponseType(typeof(ApiResponse<FoSnapshotDto>), 200)]
    public async Task<IActionResult> GetBankNiftyFo(CancellationToken ct)
    {
        var result = await foService.GetBankNiftyFoAsync(ct);
        return Ok(ApiResponse<FoSnapshotDto>.Ok(result));
    }
}