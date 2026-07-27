using Pedidos.Application.Abstracciones;

namespace Pedidos.Application.Pedidos.CancelarPedido;

public class CancelarPedidoHandler : ICommandHandler<CancelarPedidoCommand, bool>
{
    private readonly IPedidoRepository _repositorio;

    public CancelarPedidoHandler(IPedidoRepository repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<bool> Handle(CancelarPedidoCommand command, CancellationToken cancellationToken)
    {
        var pedido = await _repositorio.ObtenerPorId(command.PedidoId, cancellationToken);

        if (pedido is null)
            return false;

        pedido.Cancelar();

        await _repositorio.GuardarCambios(cancellationToken);
        return true;
    }
}