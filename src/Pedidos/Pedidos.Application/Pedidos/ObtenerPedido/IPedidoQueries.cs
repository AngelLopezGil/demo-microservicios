namespace Pedidos.Application.Pedidos.ObtenerPedido;

public interface IPedidoQueries
{
    Task<PedidoDto?> ObtenerPorId(Guid id, CancellationToken cancellationToken);
}