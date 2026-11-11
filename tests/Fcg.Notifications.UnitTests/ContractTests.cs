using Fcg.Contracts.Events;
using MassTransit;

namespace Fcg.Notifications.UnitTests;

public sealed class ContractTests
{
    [Theory]
    [InlineData(typeof(UserCreatedEvent), "fcg.user-created")]
    [InlineData(typeof(PaymentProcessedEvent), "fcg.payment-processed")]
    public void Evento_MantemIdentidadeDoContrato(Type type, string entityName)
    {
        Assert.Equal("Fcg.Contracts.Events", type.Namespace);
        Assert.Equal(entityName, type.GetCustomAttributes(typeof(EntityNameAttribute), false)
            .Cast<EntityNameAttribute>().Single().EntityName);
    }
}
