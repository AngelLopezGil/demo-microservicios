using Inventario.Api;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<InventarioDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("InventarioDb")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ReservaStockConsumer>();

    x.UsingRabbitMq((contexto, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("admin");
            h.Password("Demo_Password123!");
        });
        cfg.ConfigureEndpoints(contexto);
    });
});

var app = builder.Build();

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