using Pedidos.Domain.Excepciones;

namespace Pedidos.Domain.Pedidos;

public class Pedido
{
    private readonly List<LineaPedido> _lineas;

    public Guid Id {get; private set;}
    public Guid ClienteId {get; private set;}
    public EstadoPedido Estado {get; private set;}
    public IReadOnlyCollection<LineaPedido> Lineas => _lineas.AsReadOnly();

    private Pedido (Guid clienteId, List<LineaPedido> lineas)
    {
        Id = Guid.NewGuid();
        ClienteId = clienteId;
        _lineas = lineas;
        Estado = EstadoPedido.Pendiente;
    }

    private Pedido()
    {
        _lineas = new List<LineaPedido>();
    }

    public static Pedido Crear (Guid clienteId, IEnumerable<LineaPedido> lineas)
    {
        var listaLineas = lineas?.ToList() ?? new List<LineaPedido>();

        if (listaLineas.Count <= 0)
            throw new ExcepcionDeDominio("Un pedido debe tener al menos una linea.");

        return new Pedido(clienteId, listaLineas);
    }

    public void Confirmar()
    {
        if (Estado != EstadoPedido.Pendiente)
            throw new ExcepcionDeDominio("El pedido no esta en estado Pendiente");

        Estado = EstadoPedido.Confirmado;
    }

    public void Cancelar()
    {
        if(Estado != EstadoPedido.Pendiente)
            throw new ExcepcionDeDominio("El pedido no esta en estado Pendiente");

        Estado = EstadoPedido.Cancelado;
    }

    public decimal CalcularTotal()
    {
        return _lineas.Sum(l => l.Cantidad * l.PrecioUnitario);
    }


}