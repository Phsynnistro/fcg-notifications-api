using System.Globalization;
using Fcg.Contracts.Events;
using Fcg.Notifications.Api.Application.Abstractions;

namespace Fcg.Notifications.Api.Application.Notifications;

/// <summary>Conteúdo dos e-mails. Funções puras: fáceis de testar, sem dependência de infraestrutura.</summary>
public static class EmailTemplates
{
    public static EmailMessage Welcome(UserCreatedEvent user) => new(
        To: user.Email,
        Subject: "Bem-vindo(a) à FIAP Cloud Games!",
        Body: $"""
            Olá, {user.Name}!

            Sua conta na FIAP Cloud Games foi criada em {user.CreatedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)} (UTC).
            Explore o catálogo e comece a montar a sua biblioteca de jogos.

            Equipe FCG
            """);

    public static EmailMessage PurchaseConfirmation(PaymentProcessedEvent payment) => new(
        To: payment.UserEmail,
        Subject: $"Compra confirmada: {payment.GameTitle}",
        Body: $"""
            Olá!

            Seu pagamento foi aprovado e o jogo já está na sua biblioteca.

              Jogo:   {payment.GameTitle}
              Valor:  R$ {FormatBrl(payment.Price)}
              Pedido: {payment.OrderId}

            Bom jogo!
            Equipe FCG
            """);

    // A imagem chiseled roda com globalização invariante (sem ICU), então formatamos o padrão BR manualmente.
    private static string FormatBrl(decimal value) =>
        value.ToString("#,##0.00", CultureInfo.InvariantCulture).Replace(',', '_').Replace('.', ',').Replace('_', '.');
}
