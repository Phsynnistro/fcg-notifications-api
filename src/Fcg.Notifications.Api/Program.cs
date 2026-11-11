using Fcg.Notifications.Api;
using Fcg.Notifications.Api.Presentation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddNotifications(builder.Configuration);

var app = builder.Build();

app.MapHealthEndpoints();
app.MapGet("/", () => Results.Ok(new
{
    service = "fcg-notifications-api",
    description = "Consome UserCreatedEvent e PaymentProcessedEvent e envia e-mails (simulados no log)."
}));

await app.RunAsync();
