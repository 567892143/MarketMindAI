using Microsoft.AspNetCore.SignalR;

namespace MarketMind.API.Hubs;

// Watches for the morning briefing and notifies all clients
// when it is ready — Angular then auto-refreshes the briefing card
public class BriefingNotificationService(
    IHubContext<MarketHub> hubContext,
    ILogger<BriefingNotificationService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var utcNow = DateTime.UtcNow;

            // Briefing generated at 7:45 AM IST = 2:15 AM UTC
            // Notify clients at 8:00 AM IST = 2:30 AM UTC
            var todayNotifyTime = utcNow.Date.AddHours(2).AddMinutes(30);

            // If past today's time, schedule for tomorrow
            if (utcNow > todayNotifyTime)
                todayNotifyTime = todayNotifyTime.AddDays(1);

            var delay = todayNotifyTime - utcNow;

            logger.LogInformation(
                "[SignalR] Briefing notification scheduled in {Hours}h {Minutes}m",
                (int)delay.TotalHours, delay.Minutes);

            // Wait until notification time
            await Task.Delay(delay, ct);

            // Only notify on weekdays
            if (DateTime.UtcNow.DayOfWeek is not
                (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                var notification = new BriefingReadyDto(
                    "Today's pre-market briefing is ready",
                    DateTime.UtcNow,
                    true // Will be overridden by real briefing sentiment
                );

                // Push to ALL connected clients
                await hubContext.Clients.All
                    .SendAsync("BriefingReady", notification, ct);

                logger.LogInformation(
                    "[SignalR] Briefing ready notification sent to all clients");
            }
        }
    }
}