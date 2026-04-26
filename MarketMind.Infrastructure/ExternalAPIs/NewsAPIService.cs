using MarketMind.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MarketMind.Infrastructure.ExternalAPIs;

public class NewsAPIService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<NewsAPIService> logger) : INewsIngestionService
{
    public async Task<List<RawNewsItemDto>> FetchLatestAsync(
        string query,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var apiKey = configuration["NewsApi:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "your-newsapi-key")
        {
            logger.LogWarning("NewsAPI key not configured — returning empty list");
            return [];
        }

        try
        {
            // NewsAPI free tier — English articles sorted by date
            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"https://newsapi.org/v2/everything" +
                      $"?q={encodedQuery}" +
                      $"&language=en" +
                      $"&sortBy=publishedAt" +
                      $"&pageSize={pageSize}" +
                      $"&apiKey={apiKey}";

            var json     = await httpClient.GetStringAsync(url, ct);
            var articles = ParseNewsResponse(json);

            logger.LogInformation(
                "NewsAPI returned {Count} articles for query: {Query}",
                articles.Count, query);

            return articles;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "NewsAPI fetch failed for query: {Query}", query);
            return [];
        }
    }

    private static List<RawNewsItemDto> ParseNewsResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root      = doc.RootElement;

        if (!root.TryGetProperty("articles", out var articles))
            return [];

        var result = new List<RawNewsItemDto>();

        foreach (var article in articles.EnumerateArray())
        {
            // Skip articles with removed content
            var content = article.TryGetProperty("content", out var c)
                ? c.GetString() ?? string.Empty
                : string.Empty;

            var title = article.TryGetProperty("title", out var t)
                ? t.GetString() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(title) || title == "[Removed]")
                continue;

            var source = article.TryGetProperty("source", out var s) &&
                         s.TryGetProperty("name", out var sn)
                ? sn.GetString() ?? "Unknown"
                : "Unknown";

            var url = article.TryGetProperty("url", out var u)
                ? u.GetString() ?? string.Empty
                : string.Empty;

            var description = article.TryGetProperty("description", out var d)
                ? d.GetString() ?? string.Empty
                : string.Empty;

            // Combine description + content for richer embedding text
            var rawText = $"{title}. {description} {content}"
                .Replace("[+", "")
                .Trim();

            var publishedAt = article.TryGetProperty("publishedAt", out var p) &&
                              DateTime.TryParse(p.GetString(), out var dt)
                ? dt.ToUniversalTime()
                : DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(url))
                result.Add(new RawNewsItemDto(title, source, rawText, url, publishedAt));
        }

        return result;
    }
}