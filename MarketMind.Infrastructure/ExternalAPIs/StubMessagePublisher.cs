using MarketMind.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MarketMind.Infrastructure.Messaging;

// Logs instead of sending to Service Bus
// Real ServiceBusPublisher wired in Phase 11
public class StubMessagePublisher(
    ILogger<StubMessagePublisher> logger) : IMessagePublisher
{
    public Task PublishAsync<T>(
        string queueName,
        T message,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "[STUB] Message published to queue '{Queue}': {Message}",
            queueName,
            System.Text.Json.JsonSerializer.Serialize(message));

        return Task.CompletedTask;
    }
}