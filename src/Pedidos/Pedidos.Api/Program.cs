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

var builder = WebApplication.CreateBuilder(args);

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
        cfg.Host("localhost", "/", h =>
        {
            h.Username("admin");
            h.Password("Demo_Password123!");
        });
        cfg.ConfigureEndpoints(contexto);
    });
});

builder.Services.AddScoped<IPublicadorEventos, PublicadorEventosMassTransit>();

var app = builder.Build();
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