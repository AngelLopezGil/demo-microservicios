namespace Inventario.Api;

public class Stock
{
    public Guid ProductoId { get; set; }
    public int Disponible { get; set; }
    public int Reservado { get; set; }
}