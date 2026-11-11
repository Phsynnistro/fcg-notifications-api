using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fcg.Notifications.Api.Presentation;

/// <summary>
/// Três endpoints com semânticas DIFERENTES (é aqui que muita gente erra):
///  - /health/live  → o processo está vivo? NÃO checa dependências. Se checasse o banco,
///                    uma queda do banco faria o Kubernetes reiniciar todos os pods à toa.
///  - /health/ready → posso receber tráfego agora? Checa só o indispensável (tag "readiness").
///  - /health       → diagnóstico completo em JSON (inclui o bus do MassTransit).
/// </summary>
public static class HealthEndpoints
{
    public const string ReadinessTag = "readiness";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            // Serviço só-consumidor: "pronto" = conectado ao RabbitMQ (health check do MassTransit, tag "masstransit").
            // Assim o rolling update só derruba a réplica antiga depois que a nova já está consumindo.
            // (Nas APIs com outbox, o broker fica FORA da readiness: elas aceitam requisições mesmo com o broker fora.)
            Predicate = check => check.Tags.Contains(ReadinessTag) || check.Tags.Contains("masstransit")
        });

        app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteJsonAsync });

        return app;
    }

    private static Task WriteJsonAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = entry.Value.Duration.TotalMilliseconds,
                tags = entry.Value.Tags
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
