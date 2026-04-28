using System.Collections.Concurrent;

namespace MarketMind.API.Middleware;

// Simple in-memory rate limiter
// Protects Gemini API key from being hammered
// 10 AI requests per IP per minute
public class RateLimitingMiddleware(
    RequestDelegate next,
    ILogger<RateLimitingMiddleware> logger)
{
    private static readonly ConcurrentDictionary<string, RateLimitEntry>
        Entries = new();

    private const int MaxRequests = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    // Only rate limit AI endpoints — not health or snapshots
    private static readonly string[] ProtectedPaths =
    [
        "/api/v1/market/briefing",
        "/api/v1/sectors/",
        "/api/v1/analysis/"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var isProtected = ProtectedPaths
            .Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (!isProtected)
        {
            await next(context);
            return;
        }

        var ip    = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var entry = Entries.GetOrAdd(ip, _ => new RateLimitEntry());

        lock (entry)
        {
            var now = DateTime.UtcNow;

            // Reset window if expired
            if (now - entry.WindowStart > Window)
            {
                entry.WindowStart = now;
                entry.Count       = 0;
            }

            if (entry.Count >= MaxRequests)
            {
                logger.LogWarning(
                    "[RateLimit] IP {IP} exceeded limit on {Path}", ip, path);

                context.Response.StatusCode  = 429;
                context.Response.ContentType = "application/json";

                context.Response.WriteAsync(
                    """{"success":false,"error":"Rate limit exceeded. Try again in 1 minute."}""");

                return;
            }

            entry.Count++;
        }

        await next(context);
    }

    private class RateLimitEntry
    {
        public DateTime WindowStart { get; set; } = DateTime.UtcNow;
        public int      Count       { get; set; } = 0;
    }
}