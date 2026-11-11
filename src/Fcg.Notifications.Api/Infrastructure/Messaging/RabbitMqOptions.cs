namespace Fcg.Notifications.Api.Infrastructure.Messaging;

/// <summary>Host/porta/vhost vêm do ConfigMap; usuário/senha vêm do Secret.</summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = "localhost";
    public ushort Port { get; init; } = 5672;
    public string VirtualHost { get; init; } = "/";
    public string Username { get; init; } = "guest";
    public string Password { get; init; } = "guest";
}
