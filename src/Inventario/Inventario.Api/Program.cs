using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using MassTransit.Logging;
using Inventario.Api;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(cfg => cfg
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Azure", Serilog.Events.LogEventLevel.Warning)
    .Enrich.WithProperty("Servicio", "Inventario")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{Servicio}] {Message:lj}{NewLine}{Exception}"),
    writeToProviders: true);

builder.Services.AddOpenTelemetry()
    .UseAzureMonitor(o => o.Credential = new DefaultAzureCredential())
    .WithTracing(t => t.AddSource(DiagnosticHeaders.DefaultListenerName));

builder.Services.AddDbContext<InventarioDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("InventarioDb")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(x =>
{
    x.DisableUsageTelemetry();
    x.AddConsumer<ReservaStockConsumer>();

    x.AddEntityFrameworkOutbox<InventarioDbContext>(o => o.UseSqlServer());

    x.AddConfigureEndpointsCallback((context, name, cfg) =>
        cfg.UseEntityFrameworkOutbox<InventarioDbContext>(context));

    x.UsingAzureServiceBus((contexto, cfg) =>
    {
        cfg.Host(new Uri("sb://sb-demo-alg-7421.servicebus.windows.net"));
        cfg.ConfigureEndpoints(contexto);
    });
});

var app = builder.Build();
app.UseSerilogRequestLogging();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/stock", async (InventarioDbContext db, CancellationToken ct) =>
{
    var stocks = await db.Stocks.AsNoTracking().ToListAsync(ct);
    return Results.Ok(stocks);
});

app.Run();