using Fcg.Contracts.Events;
using Fcg.Notifications.Api.Application.Abstractions;
using Fcg.Notifications.Api.Application.Notifications;
using MassTransit;

namespace Fcg.Notifications.Api.Presentation.Consumers;

/// <summary>Fluxo de compra, passo 4: PaymentProcessedEvent (Approved) → e-mail de confirmação.</summary>
public sealed class PaymentProcessedConsumer(IEmailSender emailSender, ILogger<PaymentProcessedConsumer> logger)
    : IConsumer<PaymentProcessedEvent>
{
    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var payment = context.Message;

        if (payment.Status != PaymentStatus.Approved)
        {
            // Requisito: notificar apenas compras aprovadas. (Evolução natural: e-mail de "pagamento recusado".)
            logger.LogInformation("Pagamento do pedido {OrderId} recusado ({Reason}); nenhum e-mail de confirmação enviado.",
                payment.OrderId, payment.Reason);
            return;
        }

        await emailSender.SendAsync(EmailTemplates.PurchaseConfirmation(payment), context.CancellationToken);
    }
}
