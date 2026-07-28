using Contratos;
using MassTransit;

namespace Notificaciones.Worker;

public class NotificarConfirmacionConsumer : IConsumer<StockReservado>
{
    private readonly EmisorEmails _emails;
    private readonly ILogger<NotificarConfirmacionConsumer> _logger;

    public NotificarConfirmacionConsumer(EmisorEmails emails,
                                         ILogger<NotificarConfirmacionConsumer> logger)
    {
        _emails = emails;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockReservado> context)
    {
        await _emails.Enviar(
            asunto: $"Pedido {context.Message.PedidoId} confirmado",
            cuerpo: "Tu pedido tiene stock reservado y está confirmado. ¡Gracias!",
            context.CancellationToken);

        _logger.LogInformation("Email de confirmación enviado para {PedidoId}",
            context.Message.PedidoId);
    }
}