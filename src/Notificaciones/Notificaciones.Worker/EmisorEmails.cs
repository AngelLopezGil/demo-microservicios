using Azure;
using Azure.Communication.Email;

namespace Notificaciones.Worker;

public class EmisorEmails
{
    private readonly EmailClient _cliente;
    private readonly string _remitente;
    private readonly string _destinatario;

    public EmisorEmails(IConfiguration configuration)
    {
        var cadena = configuration.GetConnectionString("Email")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:Email");
        _remitente = configuration["Email:Remitente"]
            ?? throw new InvalidOperationException("Falta Email:Remitente");
        _destinatario = configuration["Email:Destinatario"]
            ?? throw new InvalidOperationException("Falta Email:Destinatario");

        _cliente = new EmailClient(cadena);
    }

    public async Task Enviar(string asunto, string cuerpo, CancellationToken cancellationToken)
    {
        await _cliente.SendAsync(
            WaitUntil.Completed,
            senderAddress: _remitente,
            recipientAddress: _destinatario,
            subject: asunto,
            htmlContent: null,
            plainTextContent: cuerpo,
            cancellationToken: cancellationToken);
    }
}