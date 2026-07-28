using Contratos;
using MassTransit;

namespace Notificaciones.Worker;

public class NotificarCancelacionConsumer : IConsumer<StockRechazado>
{
    private readonly EmisorEmails _emails;
    private readonly ILogger<NotificarCancelacionConsumer> _logger;

    public NotificarCancelacionConsumer(EmisorEmails emails,
                                         ILogger<NotificarCancelacionConsumer> logger)
    {
        _emails = emails;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockRechazado> context)
    {
        await _emails.Enviar(
            asunto: $"Pedido {context.Message.PedidoId} cancelado",
            cuerpo: "Tu pedido está cancelado.",
            context.CancellationToken);

        _logger.LogInformation("Email de cancelacion enviado para {PedidoId}",
            context.Message.PedidoId);
    }
}