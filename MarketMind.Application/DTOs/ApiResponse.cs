namespace MarketMind.Application.DTOs;

// Consistent envelope for every API response
// Angular can always check Success before reading Data
public record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Error,
    DateTime Timestamp
)
{
    public static ApiResponse<T> Ok(T data) =>
        new(true, data, null, DateTime.UtcNow);

    public static ApiResponse<T> Fail(string error) =>
        new(false, default, error, DateTime.UtcNow);
}