namespace Pedidos.Domain.Excepciones;
public class ExcepcionDeDominio : Exception
{
    public ExcepcionDeDominio(string mensaje) : base(mensaje) { }
}