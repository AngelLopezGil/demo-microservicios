// Pedidos/CrearPedido/CrearPedidoHandler.cs
using Pedidos.Application.Abstracciones;
using Pedidos.Domain.Pedidos;

namespace Pedidos.Application.Pedidos.CrearPedido;

public class CrearPedidoHandler : ICommandHandler<CrearPedidoCommand, Guid>
{
    private readonly IPedidoRepository _repositorio;

    public CrearPedidoHandler(IPedidoRepository repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<Guid> Handle(CrearPedidoCommand command, CancellationToken cancellationToken)
    {
        var lineaPedido = command.Lineas.Select(l => new LineaPedido(l.ProductoId, l.Cantidad, l.PrecioUnitario));

        var pedido = Pedido.Crear(command.ClienteId, lineaPedido);

        await _repositorio.Agregar(pedido,cancellationToken);
        await _repositorio.GuardarCambios(cancellationToken);

        return pedido.Id;
    }
}