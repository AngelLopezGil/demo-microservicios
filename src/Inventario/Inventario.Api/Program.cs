using Inventario.Api;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(cfg => cfg
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.WithProperty("Servicio", "Inventario")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{Servicio}] {Message:lj}{NewLine}{Exception}"));

builder.Services.AddDbContext<InventarioDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("InventarioDb")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ReservaStockConsumer>();

    x.UsingRabbitMq((contexto, cfg) =>
    {
        cfg.Host(builder.Configuration["Rabbit:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["Rabbit:Usuario"] ?? "admin");
            h.Password(builder.Configuration["Rabbit:Password"] ?? "Demo_Password123!");
        });
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