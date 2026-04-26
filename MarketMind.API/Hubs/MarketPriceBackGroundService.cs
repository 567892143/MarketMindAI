using MarketMind.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace MarketMind.API.Hubs;

// BackgroundService = runs for the lifetime of the app
// IHubContext = lets us push to SignalR clients from outside a hub
// This is the KEY pattern — most SignalR pushes come from background services
// not from the hub itself
public class MarketPriceBackgroundService(
    IHubContext<MarketHub> hubContext,
    IServiceProvider       serviceProvider,
    ILogger<MarketPriceBackgroundService> logger)
    : BackgroundService
{
    // Push intervals — balance freshness vs API rate limits
    private static readonly TimeSpan IndexInterval      = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan CommodityInterval  = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("[SignalR] Market price pusher started");

        // Run index and commodity pushers concurrently
        await Task.WhenAll(
            RunIndexPusherAsync(ct),
            RunCommodityPusherAsync(ct)
        );
    }

    // ── Index pusher ──────────────────────────────────────────────
    private async Task RunIndexPusherAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Only push during market-relevant hours
                // Indian market: 9:15 AM – 3:30 PM IST (3:45 AM – 10:00 AM UTC)
                // Pre-market:    7:00 AM – 9:15 AM IST (1:30 AM – 3:45 AM UTC)
                var utcNow    = DateTime.UtcNow;
                var isActive  = IsMarketActive(utcNow);

                if (isActive)
                {
                    await PushIndexPricesAsync(ct);
                    await PushMarketStatusAsync(ct);
                }
                else
                {
                    // Outside hours — push status only, less frequently
                    await PushMarketStatusAsync(ct);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SignalR] Index pusher error");
            }

            await Task.Delay(IndexInterval, ct);
        }
    }

    // ── Commodity pusher ──────────────────────────────────────────
    private async Task RunCommodityPusherAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await PushCommodityPricesAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SignalR] Commodity pusher error");
            }

            await Task.Delay(CommodityInterval, ct);
        }
    }

    // ── Push methods ──────────────────────────────────────────────

    private async Task PushIndexPricesAsync(CancellationToken ct)
    {
        // IMarketDataService is scoped — must create scope to resolve it
        // BackgroundService is singleton — this is the correct pattern
        using var scope      = serviceProvider.CreateScope();
        var marketData       = scope.ServiceProvider
            .GetRequiredService<IMarketDataService>();

        var indexSymbols = new[] { "NIFTY", "BANKNIFTY", "GIFT_NIFTY" };

        foreach (var symbol in indexSymbols)
        {
            try
            {
                var snapshot = await marketData.GetSnapshotAsync(symbol, ct);

                var update = new PriceUpdateDto(
                    snapshot.Symbol,
                    snapshot.DisplayName,
                    snapshot.Price,
                    snapshot.ChangePercent,
                    snapshot.ChangeAbsolute,
                    snapshot.IsBullish,
                    snapshot.DirectionLabel,
                    DateTime.UtcNow
                );

                // Push to "indices" group — only subscribed clients receive this
                await hubContext.Clients
                    .Group("indices")
                    .SendAsync("PriceUpdate", update, ct);

                logger.LogDebug(
                    "[SignalR] Pushed {Symbol}: {Price} {Direction}{Change}%",
                    symbol, snapshot.Price,
                    snapshot.DirectionLabel, snapshot.ChangePercent);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "[SignalR] Failed to push {Symbol}", symbol);
            }

            // Small delay between symbols to avoid rate limiting
            await Task.Delay(500, ct);
        }
    }

    private async Task PushCommodityPricesAsync(CancellationToken ct)
    {
        using var scope  = serviceProvider.CreateScope();
        var marketData   = scope.ServiceProvider
            .GetRequiredService<IMarketDataService>();

        var commoditySymbols = new[] { "CRUDE", "GOLD", "SILVER", "USDINR" };

        foreach (var symbol in commoditySymbols)
        {
            try
            {
                var snapshot = await marketData.GetSnapshotAsync(symbol, ct);

                var update = new PriceUpdateDto(
                    snapshot.Symbol,
                    snapshot.DisplayName,
                    snapshot.Price,
                    snapshot.ChangePercent,
                    snapshot.ChangeAbsolute,
                    snapshot.IsBullish,
                    snapshot.DirectionLabel,
                    DateTime.UtcNow
                );

                await hubContext.Clients
                    .Group("commodities")
                    .SendAsync("PriceUpdate", update, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "[SignalR] Failed to push {Symbol}", symbol);
            }

            await Task.Delay(500, ct);
        }
    }

    private async Task PushMarketStatusAsync(CancellationToken ct)
    {
        var status = GetMarketStatus();

        await hubContext.Clients
            .All
            .SendAsync("MarketStatus", status, ct);
    }

    // ── Market hours logic ────────────────────────────────────────

    private static bool IsMarketActive(DateTime utcNow)
    {
        // Monday–Friday only
        if (utcNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return false;

        // IST = UTC + 5:30
        // Pre-market starts 7:00 AM IST = 1:30 AM UTC
        // Market closes   3:30 PM IST = 10:00 AM UTC
        var utcPreMarketStart = TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(30));
        var utcMarketClose    = TimeSpan.FromHours(10);

        var timeOfDay = utcNow.TimeOfDay;
        return timeOfDay >= utcPreMarketStart && timeOfDay <= utcMarketClose;
    }

    private static MarketStatusDto GetMarketStatus()
    {
        var utcNow   = DateTime.UtcNow;
        var ist      = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows()
                ? "India Standard Time"
                : "Asia/Kolkata");
        var istNow   = TimeZoneInfo.ConvertTimeFromUtc(utcNow, ist);
        var timeOfDay = istNow.TimeOfDay;

        var isWeekday = istNow.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

        // Market sessions in IST
        var preMarketStart = new TimeSpan(7,  0, 0);
        var marketOpen     = new TimeSpan(9, 15, 0);
        var marketClose    = new TimeSpan(15, 30, 0);

        if (!isWeekday)
            return new MarketStatusDto(false, "Closed",
                GetNextMonday(istNow), "Market opens on");

        if (timeOfDay < preMarketStart)
            return new MarketStatusDto(false, "Closed",
                istNow.Date.Add(preMarketStart), "Pre-market opens in");

        if (timeOfDay < marketOpen)
            return new MarketStatusDto(false, "Pre-Market",
                istNow.Date.Add(marketOpen), "Market opens in");

        if (timeOfDay < marketClose)
            return new MarketStatusDto(true, "Market",
                istNow.Date.Add(marketClose), "Market closes in");

        return new MarketStatusDto(false, "Post-Market",
            GetNextMarketOpen(istNow), "Market opens tomorrow at");
    }

    private static DateTime GetNextMonday(DateTime ist)
    {
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)ist.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7;
        return ist.Date.AddDays(daysUntilMonday).AddHours(9).AddMinutes(15);
    }

    private static DateTime GetNextMarketOpen(DateTime ist)
    {
        var next = ist.Date.AddDays(1);
        while (next.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            next = next.AddDays(1);
        return next.AddHours(9).AddMinutes(15);
    }
}