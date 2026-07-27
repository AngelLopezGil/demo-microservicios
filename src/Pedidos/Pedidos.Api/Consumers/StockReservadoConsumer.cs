using Contratos;
using MassTransit;
using Pedidos.Application.Pedidos.ConfirmarPedido;

namespace Pedidos.Api.Consumers;

public class StockReservadoConsumer : IConsumer<StockReservado>
{
    private readonly ConfirmarPedidoHandler _handler;
    private readonly ILogger<StockReservadoConsumer> _logger;

    public StockReservadoConsumer(ConfirmarPedidoHandler handler,
                                  ILogger<StockReservadoConsumer> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockReservado> context)
    {
        var confirmado = await _handler.Handle(
            new ConfirmarPedidoCommand(context.Message.PedidoId),
            context.CancellationToken);

        if (confirmado)
            _logger.LogInformation("Pedido {PedidoId} confirmado por reserva de stock",
                context.Message.PedidoId);
        else
            _logger.LogWarning("StockReservado para pedido {PedidoId} que no existe",
                context.Message.PedidoId);
    }
}