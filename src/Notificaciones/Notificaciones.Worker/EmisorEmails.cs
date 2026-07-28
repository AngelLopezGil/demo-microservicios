using System.Net.Mail;

namespace Notificaciones.Worker;

public class EmisorEmails
{
    public async Task Enviar(string asunto, string cuerpo, CancellationToken cancellationToken)
    {
        using var cliente = new SmtpClient("localhost", 1025);
        using var mensaje = new MailMessage(
            from: "pedidos@demo.local",
            to: "cliente@demo.local",
            asunto, cuerpo);
        await cliente.SendMailAsync(mensaje, cancellationToken);
    }
}