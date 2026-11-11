using MassTransit;

// ATENÇÃO: contrato público compartilhado por CÓPIA entre PaymentsAPI (publica), CatalogAPI e
// NotificationsAPI (consomem). Evolua apenas de forma ADITIVA.
namespace Fcg.Contracts.Events;

/// <summary>Resultado do pagamento. Serializado como texto ("Approved"/"Rejected") no barramento.</summary>
public enum PaymentStatus
{
    Approved = 1,
    Rejected = 2
}

/// <summary>Publicado pelo PaymentsAPI após processar um <see cref="OrderPlacedEvent"/>.</summary>
[EntityName("fcg.payment-processed")]
public sealed record PaymentProcessedEvent(
    Guid OrderId,
    Guid UserId,
    Guid GameId,
    decimal Price,
    PaymentStatus Status,
    string? Reason,
    string UserEmail,
    string GameTitle,
    DateTime ProcessedAt);
