using Pedidos.Application.Abstracciones;
using Pedidos.Domain.Pedidos;
using Contratos;
namespace Pedidos.Application.Pedidos.CrearPedido;

public class CrearPedidoHandler : ICommandHandler<CrearPedidoCommand, Guid>
{
    private readonly IPedidoRepository _repositorio;
    private readonly IPublicadorEventos _eventos;

    public CrearPedidoHandler(IPedidoRepository repositorio, IPublicadorEventos publicadorEventos)
    {
        _repositorio = repositorio;
        _eventos = publicadorEventos;
    }

    public async Task<Guid> Handle(CrearPedidoCommand command, CancellationToken cancellationToken)
    {
        var lineaPedido = command.Lineas.Select(l => new LineaPedido(l.ProductoId, l.Cantidad, l.PrecioUnitario));

        var pedido = Pedido.Crear(command.ClienteId, lineaPedido);

        var pedidoLineas = pedido.Lineas.Select(l => new LineaPedidoCreado(l.ProductoId, l.Cantidad)).ToList();

        await _repositorio.Agregar(pedido,cancellationToken);
        var evento = new PedidoCreado(pedido.Id, pedido.ClienteId, pedidoLineas);
        await _eventos.Publicar(evento, cancellationToken);
        await _repositorio.GuardarCambios(cancellationToken);

        return pedido.Id;
    }
}