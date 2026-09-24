using Azure;
using Azure.Communication.Email;
using Azure.Identity;

namespace Notificaciones.Worker;

public class EmisorEmails
{
    private readonly EmailClient _cliente;
    private readonly string _remitente;
    private readonly string _destinatario;

    public EmisorEmails(IConfiguration configuration)
    {
        var endpoint = configuration["Email:Endpoint"]
            ?? throw new InvalidOperationException("Falta Email:Endpoint");
        _remitente = configuration["Email:Remitente"]
            ?? throw new InvalidOperationException("Falta Email:Remitente");
        _destinatario = configuration["Email:Destinatario"]
            ?? throw new InvalidOperationException("Falta Email:Destinatario");

        _cliente = new EmailClient(new Uri(endpoint), new DefaultAzureCredential());
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