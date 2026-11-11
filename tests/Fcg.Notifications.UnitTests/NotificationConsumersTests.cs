using System.Collections.Concurrent;
using Fcg.Contracts.Events;
using Fcg.Notifications.Api.Application.Abstractions;
using Fcg.Notifications.Api.Presentation.Consumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Notifications.UnitTests;

/// <summary>
/// Testes de consumer com o test harness do MassTransit: transporte em memória, sem RabbitMQ.
/// Validam o comportamento real do pipeline (publish → consume), não só o método Consume isolado.
/// </summary>
public sealed class NotificationConsumersTests : IAsyncLifetime
{
    private readonly FakeEmailSender _emailSender = new();
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    public async Task InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IEmailSender>(_emailSender)
            .AddMassTransitTestHarness(bus =>
            {
                bus.AddConsumer<UserCreatedConsumer>();
                bus.AddConsumer<PaymentProcessedConsumer>();
            })
            .BuildServiceProvider(validateScopes: true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task UserCreated_EnviaEmailDeBoasVindas()
    {
        await _harness.Bus.Publish(new UserCreatedEvent(Guid.NewGuid(), "Ana", "ana@fcg.local", DateTime.UtcNow));

        Assert.True(await _harness.GetConsumerHarness<UserCreatedConsumer>().Consumed.Any<UserCreatedEvent>());
        var email = Assert.Single(_emailSender.Sent);
        Assert.Equal("ana@fcg.local", email.To);
        Assert.Contains("Bem-vindo", email.Subject);
        Assert.Contains("Ana", email.Body);
    }

    [Fact]
    public async Task PaymentApproved_EnviaConfirmacaoDeCompra()
    {
        await _harness.Bus.Publish(NewPayment(PaymentStatus.Approved, 1499.90m));

        Assert.True(await _harness.GetConsumerHarness<PaymentProcessedConsumer>().Consumed.Any<PaymentProcessedEvent>());
        var email = Assert.Single(_emailSender.Sent);
        Assert.Equal("bob@fcg.local", email.To);
        Assert.Contains("Pixel Quest", email.Subject);
        Assert.Contains("R$ 1.499,90", email.Body);
    }

    [Fact]
    public async Task PaymentRejected_NaoEnviaEmail()
    {
        await _harness.Bus.Publish(NewPayment(PaymentStatus.Rejected, 499.90m));

        Assert.True(await _harness.GetConsumerHarness<PaymentProcessedConsumer>().Consumed.Any<PaymentProcessedEvent>());
        Assert.Empty(_emailSender.Sent);
    }

    private static PaymentProcessedEvent NewPayment(PaymentStatus status, decimal price) => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), price, status,
        status == PaymentStatus.Rejected ? "Valor acima do limite" : null,
        "bob@fcg.local", "Pixel Quest", DateTime.UtcNow);

    private sealed class FakeEmailSender : IEmailSender
    {
        public ConcurrentQueue<EmailMessage> Sent { get; } = new();

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Sent.Enqueue(message);
            return Task.CompletedTask;
        }
    }
}
