using Pedidos.Domain.Excepciones;
using Pedidos.Domain.Pedidos;

namespace Pedidos.Tests;

public class PedidoTests
{
    private static LineaPedido LineaValida() => 
        new (Guid.NewGuid(), cantidad: 2, precioUnitario: 10m);

    [Fact]
    public void Crear_ConLineasValidas_QuedaEnEstadoPendiente()
    {
        var lineas = new [] {LineaValida()};
        
        var pedido = Pedido.Crear(Guid.NewGuid(), lineas);

        Assert.Equal(EstadoPedido.Pendiente, pedido.Estado);
        Assert.Single(pedido.Lineas);
    }

    [Fact]
    public void Crear_SinLineas_LanzaExcepcionDeDominio()
    {
        Assert.Throws<ExcepcionDeDominio>(
            () => Pedido.Crear(Guid.NewGuid(), Array.Empty<LineaPedido>()));
    }

    [Fact]
    public void Linea_ConCantidadCero_LanzaExcepcionDeDominio()
    {
        Assert.Throws<ExcepcionDeDominio>(
        () => new LineaPedido(Guid.NewGuid(), cantidad: 0, precioUnitario: 10m));
    }

    [Fact]
    public void Confirmar_PedidoPendiente_CambiaAConfirmado()
    {
        var lineas = new [] {LineaValida()};
        var pedido = Pedido.Crear(Guid.NewGuid(), lineas);

        pedido.Confirmar();

        Assert.Equal(EstadoPedido.Confirmado, pedido.Estado);
    }

    [Fact]
    public void ConfirmarCancelado_LanzaExcepcionDeDominio()
    {
        var lineas = new [] {LineaValida()};
        var pedido = Pedido.Crear(Guid.NewGuid(), lineas);

        pedido.Cancelar();

        Assert.Throws<ExcepcionDeDominio>(
            () => pedido.Confirmar());
    }

    [Fact]
    public void CalcularTotal_ConVariasLineas_SumaCantidadPorPrecio()
    {
        var lineas = new[]
        {
            new LineaPedido(Guid.NewGuid(), cantidad: 2, precioUnitario: 10m),
            new LineaPedido(Guid.NewGuid(), cantidad: 1, precioUnitario: 5m)
        };
        var pedido = Pedido.Crear(Guid.NewGuid(), lineas);

        
        Assert.Equal(25, pedido.CalcularTotal());
    }

    [Fact]
    public void Linea_ConPrecioNegativo_LanzaExcepcionDeDominio()
    {
        Assert.Throws<ExcepcionDeDominio>(
            () => new LineaPedido(Guid.NewGuid(), cantidad: 1, precioUnitario: -5m));
    }

    [Fact]
    public void Linea_ConPrecioCero_EsValida()
    {
        var linea = new LineaPedido(Guid.NewGuid(), cantidad: 1, precioUnitario: 0m);

        Assert.Equal(0m, linea.PrecioUnitario);
    }
}