using Pedidos.Domain.Excepciones;

namespace Pedidos.Domain.Pedidos;

public sealed record LineaPedido
{
    public Guid ProductoId {get;}
    public int Cantidad {get;}
    public decimal PrecioUnitario {get;}

    public LineaPedido (Guid productoId, int cantidad, decimal precioUnitario)
    {
        if (cantidad <= 0)
            throw new ExcepcionDeDominio("La cantidad debe ser mayor que cero");

        if (precioUnitario < 0)
            throw new ExcepcionDeDominio("El precio unitario no puede ser negativo");

        ProductoId = productoId;
        Cantidad = cantidad;
        PrecioUnitario = precioUnitario;
    }
}