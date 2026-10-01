using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using MassTransit.Logging;
using MassTransit;
using Notificaciones.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(cfg => cfg
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.WithProperty("Servicio", "Notificaciones")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{Servicio}] {Message:lj}{NewLine}{Exception}"));

builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddSource(DiagnosticHeaders.DefaultListenerName)
        .AddAzureMonitorTraceExporter(o => o.Credential = new DefaultAzureCredential()));

builder.Services.AddSingleton<EmisorEmails>();

builder.Services.AddMassTransit(x =>
{
    x.DisableUsageTelemetry();
    x.AddConsumer<NotificarConfirmacionConsumer>();
    x.AddConsumer<NotificarCancelacionConsumer>();

    x.UsingAzureServiceBus((contexto, cfg) =>
    {
        cfg.Host(new Uri("sb://sb-demo-alg-7421.servicebus.windows.net"));
        cfg.ConfigureEndpoints(contexto);
    });
});

var host = builder.Build();
host.Run();