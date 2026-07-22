namespace Pedidos.Application.Pedidos.CrearPedido;

public sealed record CrearPedidoCommand(Guid ClienteId, List<LineaPedidoInput> Lineas);

public sealed record LineaPedidoInput(Guid ProductoId, int Cantidad, decimal PrecioUnitario);