namespace MarketMind.Application.Interfaces;

// Implemented by NewsAPIService in Infrastructure/ExternalAPIs
// Fetches raw news — nothing else
public interface INewsIngestionService
{
    Task<List<RawNewsItemDto>> FetchLatestAsync(
        string query,
        int pageSize = 20,
        CancellationToken ct = default);
}

public record RawNewsItemDto(
    string Headline,
    string Source,
    string RawText,
    string Url,
    DateTime PublishedAt
);