using MassTransit;
using Pedidos.Application.Abstracciones;

namespace Pedidos.Infrastructure.Mensajeria;

public class PublicadorEventosMassTransit : IPublicadorEventos
{
    private readonly IPublishEndpoint _publishEndpoint;

    public PublicadorEventosMassTransit(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task Publicar<TEvento>(TEvento evento, CancellationToken cancellationToken)
        where TEvento : class
        => _publishEndpoint.Publish(evento, cancellationToken);
}