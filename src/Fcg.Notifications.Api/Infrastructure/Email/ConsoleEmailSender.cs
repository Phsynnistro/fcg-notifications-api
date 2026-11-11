using Fcg.Notifications.Api.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Fcg.Notifications.Api.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string From { get; init; } = "no-reply@fiapcloudgames.com";
}

/// <summary>"Envia" o e-mail escrevendo no log (requisito do desafio). Visível com docker compose logs / kubectl logs.</summary>
public sealed class ConsoleEmailSender(IOptions<EmailOptions> options, ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            """
            ===================== E-MAIL ENVIADO (simulado) =====================
            De:      {From}
            Para:    {To}
            Assunto: {Subject}
            ---------------------------------------------------------------------
            {Body}
            =====================================================================
            """,
            options.Value.From, message.To, message.Subject, message.Body);

        return Task.CompletedTask;
    }
}
