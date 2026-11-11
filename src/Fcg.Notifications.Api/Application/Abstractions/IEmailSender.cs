namespace Fcg.Notifications.Api.Application.Abstractions;

public sealed record EmailMessage(string To, string Subject, string Body);

/// <summary>Porta de saída. Adapter atual: console. Produção: SMTP, SendGrid, Amazon SES, Azure Communication Services.</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
