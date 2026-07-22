using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Abstracciones;
using Pedidos.Application.Pedidos.ConfirmarPedido;
using Pedidos.Application.Pedidos.CrearPedido;
using Pedidos.Infrastructure.Persistencia;

var builder = WebApplication.CreateBuilder(args);

// Persistencia: el DbContext lee la connection string de appsettings
builder.Services.AddDbContext<PedidosDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("PedidosDb")));

// Inversión de dependencias en acción: cuando alguien pida IPedidoRepository,
// el contenedor de DI entrega el PedidoRepository de EF. Application nunca lo sabrá.
builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<CrearPedidoHandler>();
builder.Services.AddScoped<ConfirmarPedidoHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Endpoint de creación: el body JSON se deserializa solo al command
app.MapPost("/pedidos", async (CrearPedidoCommand command,
                               CrearPedidoHandler handler,
                               CancellationToken ct) =>
{
    var id = await handler.Handle(command, ct);
    return Results.Created($"/pedidos/{id}", new { id });
});

// TODO (tuyo): endpoint POST /pedidos/{id}/confirmar
//  - la ruta lleva el id: app.MapPost("/pedidos/{id:guid}/confirmar", ...)
//  - la lambda recibe (Guid id, ConfirmarPedidoHandler handler, CancellationToken ct)
//  - construye el ConfirmarPedidoCommand con ese id y llama al handler
//  - si devuelve true → Results.NoContent(); si false → Results.NotFound()

app.Run();