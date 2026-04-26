using MarketMind.Application.DTOs;
using MarketMind.Application.UseCases.GetSectorAnalysis;
using MarketMind.Application.UseCases.GetSectorSentiments;
using Microsoft.AspNetCore.Mvc;

namespace MarketMind.API.Controllers;

[ApiController]
[Route("api/v1/sectors")]
[Produces("application/json")]
public class SectorController(
    GetSectorSentimentsUseCase sentimentsUseCase,
    GetSectorAnalysisUseCase   analysisUseCase) : ControllerBase
{
    /// <summary>
    /// Get sentiment scores for all sectors.
    /// Drives the heatmap on the dashboard.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SectorSentimentDto>>), 200)]
    public async Task<IActionResult> GetAllSentiments(CancellationToken ct)
    {
        var result = await sentimentsUseCase.ExecuteAsync(ct);
        return Ok(ApiResponse<List<SectorSentimentDto>>.Ok(result));
    }

    /// <summary>
    /// Get deep AI analysis for a specific sector.
    /// Sectors: IT, Banking, Energy, Pharma, Auto, FMCG, Metals, Realty
    /// </summary>
    [HttpGet("{sector}")]
    [ProducesResponseType(typeof(ApiResponse<SectorAnalysisDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GetSectorAnalysis(
        string sector,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sector))
            return BadRequest(ApiResponse<object>.Fail("Sector is required."));

        var result = await analysisUseCase.ExecuteAsync(sector, ct);
        return Ok(ApiResponse<SectorAnalysisDto>.Ok(result));
    }
}