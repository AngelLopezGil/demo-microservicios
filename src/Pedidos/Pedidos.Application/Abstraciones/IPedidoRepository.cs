using Pedidos.Domain.Pedidos;

namespace Pedidos.Application.Abstracciones;

public interface IPedidoRepository
{
    Task<Pedido?> ObtenerPorId(Guid id, CancellationToken cancellationToken);
    Task Agregar(Pedido pedido, CancellationToken cancellationToken);
    Task GuardarCambios(CancellationToken cancellationToken);
}