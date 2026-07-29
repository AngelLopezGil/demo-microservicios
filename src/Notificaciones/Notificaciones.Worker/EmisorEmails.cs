using System.Net.Mail;

namespace Notificaciones.Worker;

public class EmisorEmails
{
    private readonly string _host;
    private readonly int _puerto;

    public EmisorEmails(IConfiguration configuration)
    {
        _host = configuration["Smtp:Host"] ?? "localhost";
        _puerto = int.Parse(configuration["Smtp:Puerto"] ?? "1025");
    }

    public async Task Enviar(string asunto, string cuerpo, CancellationToken cancellationToken)
    {
        using var cliente = new SmtpClient(_host, _puerto);
        using var mensaje = new MailMessage(
            from: "pedidos@demo.local",
            to: "cliente@demo.local",
            asunto, cuerpo);
        await cliente.SendMailAsync(mensaje, cancellationToken);
    }
}