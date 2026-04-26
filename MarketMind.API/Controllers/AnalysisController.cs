using MarketMind.Application.DTOs;
using MarketMind.Application.UseCases.GetWhyMarketMoved;
using MarketMind.Application.UseCases.IngestNews;
using Microsoft.AspNetCore.Mvc;

namespace MarketMind.API.Controllers;

[ApiController]
[Route("api/v1/analysis")]
[Produces("application/json")]
public class AnalysisController(
    GetWhyMarketMovedUseCase whyUseCase,
    IngestNewsUseCase        ingestUseCase) : ControllerBase
{
    /// <summary>
    /// Get post-session "Why Market Moved Today" analysis.
    /// Available after 4:00 PM IST. Pass date as yyyy-MM-dd or leave empty for today.
    /// </summary>
    [HttpGet("why-moved")]
    [ProducesResponseType(typeof(ApiResponse<WhyMarketMovedDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GetWhyMoved(
        [FromQuery] DateTime? date,
        CancellationToken ct)
    {
        try
        {
            var targetDate = date ?? DateTime.UtcNow;
            var result     = await whyUseCase.ExecuteAsync(targetDate, ct);
            return Ok(ApiResponse<WhyMarketMovedDto>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Manually trigger news ingestion.
    /// In production this runs automatically via Hangfire every 30 min.
    /// </summary>
    [HttpPost("ingest-news")]
    [ProducesResponseType(typeof(ApiResponse<IngestNewsResult>), 200)]
    public async Task<IActionResult> IngestNews(CancellationToken ct)
    {
        var result = await ingestUseCase.ExecuteAsync(ct);
        return Ok(ApiResponse<IngestNewsResult>.Ok(result));
    }
}