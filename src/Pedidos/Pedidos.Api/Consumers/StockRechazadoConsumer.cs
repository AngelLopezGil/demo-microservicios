using Contratos;
using MassTransit;
using Pedidos.Application.Pedidos.CancelarPedido;

namespace Pedidos.Api.Consumers;

public class StockRechazadoConsumer : IConsumer<StockRechazado>
{
    private readonly CancelarPedidoHandler _handler;
    private readonly ILogger<StockRechazadoConsumer> _logger;

    public StockRechazadoConsumer(CancelarPedidoHandler handler,
                                   ILogger<StockRechazadoConsumer> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockRechazado> context)
    {
        var cancelado = await _handler.Handle(
            new CancelarPedidoCommand(context.Message.PedidoId),
            context.CancellationToken);

        if (cancelado)
            _logger.LogInformation("Pedido {PedidoId} cancelado por rechazo de stock: {Motivo}",
                context.Message.PedidoId, context.Message.Motivo);
        else
            _logger.LogWarning("StockRechazado para pedido {PedidoId} que no existe: {Motivo}",
                context.Message.PedidoId, context.Message.Motivo);
    }
}
