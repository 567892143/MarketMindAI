using MarketMind.Application.Interfaces;

namespace MarketMind.Infrastructure.ExternalAPIs;

public class StubNewsIngestionService : INewsIngestionService
{
    private static readonly List<RawNewsItemDto> FakeNews =
    [
        new(
            "Fed signals dovish stance — rate cuts likely in Q3",
            "Reuters",
            "Federal Reserve officials signaled a dovish stance in recent minutes, suggesting rate cuts could come in Q3 2025 if inflation continues to moderate. Markets reacted positively with Nasdaq gaining 1.8%.",
            "https://reuters.com/stub-1",
            DateTime.UtcNow.AddHours(-2)),

        new(
            "FII net buyers at ₹2,458 Cr — IT and Banking lead inflows",
            "Economic Times",
            "Foreign institutional investors remained net buyers at ₹2,458 crore on Thursday. IT sector attracted maximum inflows followed by Banking. DII activity was muted with net selling of ₹340 crore.",
            "https://economictimes.com/stub-2",
            DateTime.UtcNow.AddHours(-3)),

        new(
            "Crude oil falls 2% on surprise inventory build",
            "Bloomberg",
            "Crude oil prices fell 2% after EIA data showed a surprise inventory build of 4.2 million barrels. Brent crude is now trading at $87.4 per barrel. The fall is positive for Indian OMCs and paint companies.",
            "https://bloomberg.com/stub-3",
            DateTime.UtcNow.AddHours(-4)),

        new(
            "TCS wins $400M digital transformation deal from European bank",
            "Mint",
            "Tata Consultancy Services won a $400 million multi-year digital transformation deal from a leading European bank. The deal covers cloud migration and core banking modernization.",
            "https://livemint.com/stub-4",
            DateTime.UtcNow.AddHours(-5)),

        new(
            "RBI keeps repo rate unchanged at 6.5% — accommodative stance maintained",
            "Business Standard",
            "The Reserve Bank of India kept the repo rate unchanged at 6.5% in its latest MPC meeting. Governor Shaktikanta Das maintained an accommodative stance, signaling support for growth.",
            "https://business-standard.com/stub-5",
            DateTime.UtcNow.AddHours(-6)),

        new(
            "Nasdaq surges 1.8% as tech earnings beat estimates",
            "CNBC",
            "The Nasdaq Composite surged 1.8% as major tech companies reported earnings that beat analyst estimates. Microsoft and Alphabet led gains. The rally is expected to provide positive cues for Indian IT stocks.",
            "https://cnbc.com/stub-6",
            DateTime.UtcNow.AddHours(-8)),

        new(
            "USD/INR stabilizes at 83.4 — RBI intervention suspected",
            "Financial Express",
            "The Indian rupee stabilized at 83.4 against the US dollar amid suspected RBI intervention. Currency stability is positive for import-heavy sectors and reduces FII hedging costs.",
            "https://financialexpress.com/stub-7",
            DateTime.UtcNow.AddHours(-10)),
    ];

    public Task<List<RawNewsItemDto>> FetchLatestAsync(
        string query,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        // Return subset based on query keyword matching
        var results = FakeNews
            .Where(n => ContainsQueryTerms(n, query))
            .Take(pageSize)
            .ToList();

        // Always return at least 3 articles
        if (results.Count < 3)
            results = FakeNews.Take(pageSize).ToList();

        return Task.FromResult(results);
    }

    private static bool ContainsQueryTerms(RawNewsItemDto article, string query)
    {
        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return terms.Any(term =>
            article.Headline.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            article.RawText.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}