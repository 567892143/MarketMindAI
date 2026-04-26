using Microsoft.AspNetCore.Mvc;
using MarketMind.Infrastructure.Persistence;
using MarketMind.Application.Interfaces;

namespace MarketMind.API.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController(
    AppDbContext  db,
    ICacheService cache) : ControllerBase
{
    /// <summary>
    /// Health check — confirms DB and cache connectivity.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var dbOk    = false;
        var cacheOk = false;

        try
        {
            dbOk = await db.Database.CanConnectAsync(ct);
        }
        catch { /* db unreachable */ }

        try
        {
            await cache.SetAsync("health:ping", "pong", TimeSpan.FromSeconds(10), ct);
            var pong = await cache.GetAsync<string>("health:ping", ct);
            cacheOk  = pong == "pong";
        }
        catch { /* cache unreachable */ }

        var result = new
        {
            Status    = dbOk && cacheOk ? "Healthy" : "Degraded",
            Database  = dbOk    ? "Connected" : "Unreachable",
            Cache     = cacheOk ? "Connected" : "Unreachable",
            Timestamp = DateTime.UtcNow
        };

        return dbOk && cacheOk ? Ok(result) : StatusCode(503, result);
    }
}