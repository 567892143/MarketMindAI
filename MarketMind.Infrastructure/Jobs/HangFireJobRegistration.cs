using Hangfire;

namespace MarketMind.Infrastructure.Jobs;

public static class HangfireJobRegistration
{
    public static void RegisterRecurringJobs()
    {
        // All times in UTC — convert from IST (IST = UTC + 5:30)

        // Morning briefing — 7:45 AM IST = 02:15 UTC, Mon–Fri
        RecurringJob.AddOrUpdate<MorningBriefingJob>(
            "morning-briefing",
            job => job.ExecuteAsync(CancellationToken.None),
            "15 2 * * 1-5");

        // News ingestion — every 30 minutes, Mon–Fri
        // 4 queries × 30 min = 48 calls/day — within NewsAPI free tier
        RecurringJob.AddOrUpdate<NewsIngestionJob>(
            "news-ingestion",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/30 * * * 1-5");

        // Sector sentiment — every hour during market hours
        // 9 AM – 4 PM IST = 3:30 AM – 10:30 AM UTC
        RecurringJob.AddOrUpdate<SectorSentimentJob>(
            "sector-sentiment",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 3-10 * * 1-5");

        // Why Market Moved — 4:00 PM IST = 10:30 AM UTC, Mon–Fri
        RecurringJob.AddOrUpdate<WhyMarketMovedJob>(
            "why-market-moved",
            job => job.ExecuteAsync(CancellationToken.None),
            "30 10 * * 1-5");
    }
}