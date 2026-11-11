using System.Text.Json.Serialization;
using Fcg.Notifications.Api.Application.Abstractions;
using Fcg.Notifications.Api.Infrastructure.Email;
using Fcg.Notifications.Api.Infrastructure.Messaging;
using Fcg.Notifications.Api.Infrastructure.Observability;
using Fcg.Notifications.Api.Presentation.Consumers;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Fcg.Notifications.Api;

public static class NotificationsServiceCollectionExtensions
{
    public static IServiceCollection AddNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailOptions>().Bind(configuration.GetSection(EmailOptions.SectionName));
        services.AddSingleton<IEmailSender, ConsoleEmailSender>();
        services.AddObservability(configuration);

        services.AddOptions<RabbitMqOptions>().Bind(configuration.GetSection(RabbitMqOptions.SectionName));
        var messaging = configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>() ?? new MessagingOptions();

        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();

            // Duas filas, dois consumers. A fila "notifications.payment-processed" é DIFERENTE da
            // "catalog.payment-processed": as duas estão ligadas ao exchange fcg.payment-processed (fan-out),
            // então cada serviço recebe a SUA cópia do evento.
            bus.AddConsumer<UserCreatedConsumer>().Endpoint(endpoint =>
            {
                endpoint.Name = messaging.Queues.UserCreated;
                endpoint.PrefetchCount = messaging.PrefetchCount;
                endpoint.ConcurrentMessageLimit = messaging.ConcurrentMessageLimit;
            });

            bus.AddConsumer<PaymentProcessedConsumer>().Endpoint(endpoint =>
            {
                endpoint.Name = messaging.Queues.PaymentProcessed;
                endpoint.PrefetchCount = messaging.PrefetchCount;
                endpoint.ConcurrentMessageLimit = messaging.ConcurrentMessageLimit;
            });

            // Sem banco => sem inbox. Trade-off assumido: em redelivery, o e-mail pode sair duplicado.
            // Para eliminar: dar um banco ao serviço e usar UseEntityFrameworkOutbox (como Catalog/Payments).
            bus.AddConfigureEndpointsCallback((_, endpoint) =>
                endpoint.UseMessageRetry(retry =>
                    // Retry com backoff exponencial (Aula 05): até 5 novas tentativas, de 1 s a 30 s.
                    retry.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2))));

            bus.UsingRabbitMq((context, cfg) =>
            {
                var rabbit = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                cfg.Host(rabbit.Host, rabbit.Port, rabbit.VirtualHost, host =>
                {
                    host.Username(rabbit.Username);
                    host.Password(rabbit.Password);
                });

                cfg.ConfigureJsonSerializerOptions(json =>
                {
                    json.Converters.Add(new JsonStringEnumConverter());
                    return json;
                });

                // Circuit breaker do consumo (Aula 05): se 15% ou mais das mensagens falharem (após 10 no período),
                // o endpoint PARA de consumir por 30 s em vez de esgotar mensagens contra uma dependência fora do ar.
                cfg.UseKillSwitch(options => options
                    .SetActivationThreshold(10)
                    .SetTripThreshold(15)
                    .SetRestartTimeout(TimeSpan.FromSeconds(30)));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
