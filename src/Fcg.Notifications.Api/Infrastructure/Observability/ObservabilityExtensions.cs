using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Fcg.Notifications.Api.Infrastructure.Observability;

/// <summary>
/// Tracing distribuído com OpenTelemetry (Aula 06 — Observabilidade e Monitoramento).
///
/// Um único trace atravessa HTTP → PostgreSQL → RabbitMQ → outro microsserviço: o MassTransit grava o
/// contexto W3C (traceparent) nos headers da mensagem e o consumidor continua o MESMO trace.
/// No Jaeger, a compra aparece como uma árvore só: Catalog → Payments → Catalog/Notifications.
///
/// A exportação (OTLP) só é ligada quando OTEL_EXPORTER_OTLP_ENDPOINT existe (ConfigMap/compose):
/// sem coletor configurado, o serviço funciona normalmente, apenas sem exportar.
/// </summary>
public static class ObservabilityExtensions
{
    public const string ServiceName = "notifications-api";

    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var openTelemetry = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: ServiceName,
                serviceVersion: typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString()))
            .WithTracing(tracing => tracing
                .AddSource("MassTransit")          // publish/consume, inclusive a entrega do outbox
            );

        if (!string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            openTelemetry.UseOtlpExporter(); // endpoint e protocolo lidos das variáveis OTEL_* padrão
        }

        return services;
    }
}
