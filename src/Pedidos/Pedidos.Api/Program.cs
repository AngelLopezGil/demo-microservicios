using Microsoft.EntityFrameworkCore;
using Pedidos.Api;
using Pedidos.Application.Abstracciones;
using Pedidos.Application.Pedidos.CancelarPedido;
using Pedidos.Application.Pedidos.ConfirmarPedido;
using Pedidos.Application.Pedidos.CrearPedido;
using Pedidos.Application.Pedidos.ObtenerPedido;
using Pedidos.Api.Consumers;
using Pedidos.Infrastructure.Persistencia;
using MassTransit;
using Pedidos.Infrastructure.Mensajeria;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(cfg => cfg
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.WithProperty("Servicio", "Pedidos")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{Servicio}] {Message:lj}{NewLine}{Exception}"));

// Persistencia: el DbContext lee la connection string de appsettings
builder.Services.AddDbContext<PedidosDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("PedidosDb")));

// Inversión de dependencias en acción: cuando alguien pida IPedidoRepository,
// el contenedor de DI entrega el PedidoRepository de EF. Application nunca lo sabrá.
builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<CrearPedidoHandler>();
builder.Services.AddScoped<ConfirmarPedidoHandler>();
builder.Services.AddScoped<CancelarPedidoHandler>();
builder.Services.AddScoped<IPedidoQueries, PedidoQueries>();
builder.Services.AddExceptionHandler<ManejadorExcepcionesDominio>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StockReservadoConsumer>();
    x.AddConsumer<StockRechazadoConsumer>();
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

builder.Services.AddScoped<IPublicadorEventos, PublicadorEventosMassTransit>();

var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/pedidos/{id:guid}", async (Guid id, 
                                        IPedidoQueries queries,
                                        CancellationToken ct) =>
{
    var pedidoDto = await queries.ObtenerPorId(id, ct);

    if(pedidoDto is null)
        return Results.NotFound();

    return Results.Ok(pedidoDto);

});

app.MapPost("/pedidos", async (CrearPedidoCommand command,
                               CrearPedidoHandler handler,
                               CancellationToken ct) =>
{
    var id = await handler.Handle(command, ct);
    return Results.Created($"/pedidos/{id}", new { id });
});

app.MapPost("/pedidos/{id:guid}/confirmar", async (Guid id,
                                                    ConfirmarPedidoHandler handler,
                                                    CancellationToken ct) =>
{
    var confirmado = await handler.Handle(new ConfirmarPedidoCommand(id), ct);
    return confirmado ? Results.NoContent() : Results.NotFound();
});

app.Run();