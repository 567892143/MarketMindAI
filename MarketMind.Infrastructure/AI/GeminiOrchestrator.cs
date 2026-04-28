using Google.GenAI;
using MarketMind.Application.DTOs;
using MarketMind.Application.Interfaces;
using MarketMind.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MarketMind.Infrastructure.AI;

// Replaces StubAIOrchestrator
// Uses Google.GenAI SDK — same pattern as your PDF RAG project
public class GeminiOrchestrator(
    IConfiguration              configuration,
    ILogger<GeminiOrchestrator> logger) : IAIOrchestrator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── Core Gemini call — same pattern as your RagService ────────
    private async Task<string> GenerateAsync(
        string            prompt,
        CancellationToken ct = default)
    {
        var apiKey = configuration["Gemini:ApiKey"]!;
        var client = new Client(apiKey: apiKey);

        var response = await client.Models.GenerateContentAsync(
            model:    "models/gemini-2.5-flash",
            contents: prompt
        );

        var text = response.Candidates![0].Content!.Parts![0].Text
                   ?? string.Empty;

        return text.Trim();
    }

    // ── Embedding — uses REST directly (SDK may not support yet) ──
    private async Task<float[]> EmbedAsync(
        string            text,
        CancellationToken ct = default)
    {
        var apiKey = configuration["Gemini:ApiKey"]!;
        var model  = configuration["Gemini:EmbeddingModel"]
                     ?? "text-embedding-004";

        using var http = new HttpClient();
        var url = $"https://generativelanguage.googleapis.com/v1beta/" +
                  $"models/{model}:embedContent?key={apiKey}";

        var truncated = text.Length > 2000 ? text[..2000] : text;

        var body = JsonSerializer.Serialize(new
        {
            model   = $"models/{model}",
            content = new
            {
                parts = new[] { new { text = truncated } }
            }
        });

        var httpResponse = await http.PostAsync(
            url,
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
            ct);

        httpResponse.EnsureSuccessStatusCode();

        var json   = await httpResponse.Content.ReadAsStringAsync(ct);
        using var doc  = JsonDocument.Parse(json);

        var values = doc.RootElement
            .GetProperty("embedding")
            .GetProperty("values")
            .EnumerateArray()
            .Select(v => v.GetSingle())
            .ToArray();

        logger.LogDebug(
            "[Gemini] Embedding generated — {Dims} dimensions", values.Length);

        return values;
    }

    // ── IAIOrchestrator implementations ───────────────────────────

    public async Task<(string Text, int TokensUsed)>
        GeneratePreMarketBriefingAsync(
            List<MarketSnapshotDto> snapshots,
            List<NewsArticle>       relevantNews,
            CancellationToken       ct = default)
    {
        var prompt = PromptBuilder.BuildPreMarketPrompt(snapshots, relevantNews);

        logger.LogInformation(
            "[Gemini] Generating pre-market briefing — " +
            "{Snapshots} snapshots, {News} articles",
            snapshots.Count, relevantNews.Count);

        var text = await GenerateAsync(prompt, ct);

        // Estimate tokens — SDK v1.4 may not return usage metadata
        var tokensUsed = EstimateTokens(prompt) + EstimateTokens(text);

        logger.LogInformation(
            "[Gemini] Briefing generated — {Chars} chars, ~{Tokens} tokens",
            text.Length, tokensUsed);

        return (text, tokensUsed);
    }

    public async Task<(string Text, int TokensUsed)>
        GenerateSectorAnalysisAsync(
            string            sector,
            List<NewsArticle> sectorNews,
            List<TopStockDto> topStocks,
            CancellationToken ct = default)
    {
        var prompt = PromptBuilder.BuildSectorAnalysisPrompt(
            sector, sectorNews, topStocks);

        logger.LogInformation(
            "[Gemini] Generating sector analysis for {Sector}", sector);

        var text       = await GenerateAsync(prompt, ct);
        var tokensUsed = EstimateTokens(prompt) + EstimateTokens(text);

        return (text, tokensUsed);
    }

    public async Task<(string Text, int TokensUsed)>
        GenerateWhyMarketMovedAsync(
            List<OhlcPointDto> ohlc,
            List<NewsArticle>  dayNews,
            CancellationToken  ct = default)
    {
        var prompt = PromptBuilder.BuildWhyMarketMovedPrompt(ohlc, dayNews);

        logger.LogInformation("[Gemini] Generating Why Market Moved analysis");

        var text       = await GenerateAsync(prompt, ct);
        var tokensUsed = EstimateTokens(prompt) + EstimateTokens(text);

        return (text, tokensUsed);
    }

    public async Task<(string Sentiment, float Score, string[] Sectors)>
        ScoreSentimentAsync(
            string            headline,
            string            text,
            CancellationToken ct = default)
    {
        var prompt   = PromptBuilder.BuildSentimentScoringPrompt(headline, text);
        var response = await GenerateAsync(prompt, ct);

        try
        {
            // Clean markdown fences Gemini sometimes adds
            var clean = response
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            var parsed = JsonSerializer.Deserialize<SentimentResult>(
                clean, JsonOptions);

            return (
                parsed?.Sentiment ?? "Neutral",
                parsed?.Score     ?? 0.5f,
                parsed?.Sectors   ?? ["General"]
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "[Gemini] Could not parse sentiment JSON — defaulting to Neutral.\n" +
                "Response was: {Response}", response);

            return ("Neutral", 0.5f, ["General"]);
        }
    }

    // ── Token estimation ──────────────────────────────────────────
    // Rough estimate: 1 token ≈ 4 characters
    private static int EstimateTokens(string text) =>
        text.Length / 4;

    // ── Response models ───────────────────────────────────────────
    private class SentimentResult
    {
        public string   Sentiment { get; set; } = "Neutral";
        public float    Score     { get; set; } = 0.5f;
        public string[] Sectors   { get; set; } = ["General"];
    }
}