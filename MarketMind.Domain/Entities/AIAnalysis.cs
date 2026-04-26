using MarketMind.Domain.Enums;

namespace MarketMind.Domain.Entities;

public class AIAnalysis
{
    public Guid Id { get; private set; }
    public AnalysisType Type { get; private set; }

    // "premarket" | "IT_sector" | "2025-04-25"
    public string TargetKey { get; private set; } = string.Empty;
    public string ResponseText { get; private set; } = string.Empty;
    public Guid[] SourceArticleIds { get; private set; } = [];
    public int TokensUsed { get; private set; }
    public DateTime GeneratedAt { get; private set; }
    public DateTime ValidUntil { get; private set; }

    private AIAnalysis() { }

    public static AIAnalysis Create(
        AnalysisType type,
        string targetKey,
        string responseText,
        Guid[] sourceArticleIds,
        int tokensUsed,
        TimeSpan validFor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(responseText);

        return new AIAnalysis
        {
            Id = Guid.NewGuid(),
            Type = type,
            TargetKey = targetKey,
            ResponseText = responseText,
            SourceArticleIds = sourceArticleIds ?? [],
            TokensUsed = tokensUsed,
            GeneratedAt = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.Add(validFor)
        };
    }

    public bool IsExpired() => DateTime.UtcNow > ValidUntil;
}