namespace Contratos;

public sealed record LineaPedidoCreado(Guid ProductoId, int Cantidad);
public sealed record PedidoCreado(Guid PedidoId, Guid ClienteId, List<LineaPedidoCreado> Lineas);
public sealed record StockReservado(Guid PedidoId);
public sealed record StockRechazado(Guid PedidoId, string Motivo);