using Contratos;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api;

public class ReservaStockConsumer : IConsumer<PedidoCreado>
{
    private readonly InventarioDbContext _db;
    private readonly ILogger<ReservaStockConsumer> _logger;

    public ReservaStockConsumer(InventarioDbContext db, ILogger<ReservaStockConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PedidoCreado> context)
    {
        var evento = context.Message;
        _logger.LogInformation("Recibido PedidoCreado {PedidoId}", evento.PedidoId);

        // Cargamos de una vez todos los stocks implicados (un solo viaje a la BD)
        var productoIds = evento.Lineas.Select(l => l.ProductoId).ToList();
        var stocks = await _db.Stocks
            .Where(s => productoIds.Contains(s.ProductoId))
            .ToDictionaryAsync(s => s.ProductoId, context.CancellationToken);

        foreach (var linea in evento.Lineas)
        {
            if (!stocks.TryGetValue(linea.ProductoId, out var stock))
            {
                _logger.LogWarning("Producto {ProductoId} desconocido para el pedido {PedidoId}", linea.ProductoId, evento.PedidoId);
                await context.Publish(new StockRechazado(evento.PedidoId, $"Producto {linea.ProductoId} no existe en inventario"), context.CancellationToken);
                return;
            }

            if (stock.Disponible < linea.Cantidad)
            {
                _logger.LogWarning("Stock insuficiente para el producto {ProductoId} en el pedido {PedidoId}", linea.ProductoId, evento.PedidoId);
                await context.Publish(new StockRechazado(evento.PedidoId, $"Stock insuficiente para el producto {linea.ProductoId}"), context.CancellationToken);
                return;
            }
        }

        foreach (var linea in evento.Lineas)
        {
            var stock = stocks[linea.ProductoId];
            stock.Disponible -= linea.Cantidad;
            stock.Reservado += linea.Cantidad;
        }

        await _db.SaveChangesAsync(context.CancellationToken);
        await context.Publish(new StockReservado(evento.PedidoId), context.CancellationToken);

        _logger.LogInformation("Stock reservado para pedido {PedidoId}", evento.PedidoId);
    }
}