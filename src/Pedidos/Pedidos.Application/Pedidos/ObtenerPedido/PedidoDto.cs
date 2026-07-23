namespace Pedidos.Application.Pedidos.ObtenerPedido;

public sealed record PedidoDto(
    Guid Id,
    Guid ClienteId,
    string Estado,
    decimal Total,
    List<LineaDto> Lineas);

public sealed record LineaDto(Guid ProductoId, int Cantidad, decimal PrecioUnitario);