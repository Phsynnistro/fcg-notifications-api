using Fcg.Contracts.Events;
using Fcg.Notifications.Api.Application.Abstractions;
using Fcg.Notifications.Api.Application.Notifications;
using MassTransit;

namespace Fcg.Notifications.Api.Presentation.Consumers;

/// <summary>Fluxo de cadastro, passo 2: UserCreatedEvent → e-mail de boas-vindas.</summary>
public sealed class UserCreatedConsumer(IEmailSender emailSender, ILogger<UserCreatedConsumer> logger) : IConsumer<UserCreatedEvent>
{
    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        logger.LogInformation("UserCreatedEvent recebido (MessageId {MessageId}) para o usuário {UserId}.",
            context.MessageId, context.Message.UserId);

        await emailSender.SendAsync(EmailTemplates.Welcome(context.Message), context.CancellationToken);
    }
}
