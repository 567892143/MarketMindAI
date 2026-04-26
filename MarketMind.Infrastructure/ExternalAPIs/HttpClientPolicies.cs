using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace MarketMind.Infrastructure.ExternalAPIs;

// Polly retry and circuit breaker policies
// Applied to all external HTTP clients
public static class HttpClientPolicies
{
    // Retry 3 times with exponential backoff: 2s, 4s, 8s
    // Handles transient HTTP errors (5xx, 408, network failures)
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, timespan, attempt, _) =>
                {
                    Console.WriteLine(
                        $"[Polly] Retry {attempt} after {timespan.TotalSeconds}s — " +
                        $"{outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                });

    // Circuit breaker — stops calling a failing service
    // Opens after 5 failures, stays open for 30 seconds
    // Prevents hammering a down service with repeated calls
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (_, duration) =>
                    Console.WriteLine($"[Polly] Circuit OPEN for {duration.TotalSeconds}s"),
                onReset: () =>
                    Console.WriteLine("[Polly] Circuit CLOSED — service recovered"),
                onHalfOpen: () =>
                    Console.WriteLine("[Polly] Circuit HALF-OPEN — testing service"));
}