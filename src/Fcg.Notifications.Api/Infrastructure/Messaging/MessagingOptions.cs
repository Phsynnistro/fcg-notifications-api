namespace Fcg.Notifications.Api.Infrastructure.Messaging;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    public NotificationQueues Queues { get; init; } = new();
    public int PrefetchCount { get; init; } = 16;
    public int ConcurrentMessageLimit { get; init; } = 8;
}

public sealed class NotificationQueues
{
    public string UserCreated { get; init; } = "notifications.user-created";
    public string PaymentProcessed { get; init; } = "notifications.payment-processed";
}
