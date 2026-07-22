using Pedidos.Application.Abstracciones;
using Pedidos.Domain.Pedidos;

namespace Pedidos.Application.Pedidos.ConfirmarPedido;

public class ConfirmarPedidoHandler : ICommandHandler<ConfirmarPedidoCommand, bool>
{
    private readonly IPedidoRepository _repositorio;

    public ConfirmarPedidoHandler(IPedidoRepository repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<bool> Handle(ConfirmarPedidoCommand command, CancellationToken cancellationToken)
    {
        // TODO 5: obtén el pedido por command.PedidoId
        var pedido = await _repositorio.ObtenerPorId(command.PedidoId, cancellationToken);

        // TODO 6: si es null, devuelve false (la API lo convertirá en un 404)
        if (pedido is null)
            return false;

        // TODO 7: confirma el pedido (la guarda de estado ya vive en el dominio,
        //         aquí NO se repite — fíjate en lo fino que queda el handler)
        pedido.Confirmar();

        // TODO 8: guarda cambios y devuelve true
        await _repositorio.GuardarCambios(cancellationToken);
        return true;
    }
}