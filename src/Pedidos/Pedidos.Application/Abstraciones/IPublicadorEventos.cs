namespace Pedidos.Application.Abstracciones;

public interface IPublicadorEventos
{
    Task Publicar<TEvento>(TEvento evento, CancellationToken cancellationToken)
        where TEvento : class;
}