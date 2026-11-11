using MassTransit;

// ATENÇÃO: o namespace faz parte do contrato. O MassTransit identifica a mensagem pela URN
// "urn:message:Fcg.Contracts.Events:UserCreatedEvent". Os consumidores (NotificationsAPI) têm uma
// cópia idêntica deste tipo. Mudanças devem ser ADITIVAS (novas propriedades opcionais);
// renomear/remover campo é breaking change e exige nova versão do evento.
namespace Fcg.Contracts.Events;

/// <summary>Publicado pelo UsersAPI quando um usuário é cadastrado.</summary>
[EntityName("fcg.user-created")]
public sealed record UserCreatedEvent(
    Guid UserId,
    string Name,
    string Email,
    DateTime CreatedAt);
