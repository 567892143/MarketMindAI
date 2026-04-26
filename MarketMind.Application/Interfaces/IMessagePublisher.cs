namespace MarketMind.Application.Interfaces;

// Implemented by ServiceBusPublisher in Infrastructure/Messaging
// Abstracts Azure Service Bus — Application layer never knows it's Service Bus
public interface IMessagePublisher
{
    Task PublishAsync<T>(
        string queueName,
        T message,
        CancellationToken ct = default);
}