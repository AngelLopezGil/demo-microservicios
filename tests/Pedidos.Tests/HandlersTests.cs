using Pedidos.Application.Pedidos.CrearPedido;
using Pedidos.Application.Pedidos.ConfirmarPedido;
using Pedidos.Domain.Pedidos;
using System.Collections;
using Contratos;
using Pedidos.Application.Pedidos.CancelarPedido;

namespace Pedidos.Tests;

public class HandlersTests
{
    private static CrearPedidoCommand CommandValido() =>
        new(Guid.NewGuid(), new List<LineaPedidoInput>
        {
            new(Guid.NewGuid(), Cantidad: 2, PrecioUnitario: 10m)
        });

    [Fact]
    public async Task CrearPedido_ConCommandValido_GuardaElPedido()
    {
        var repositorio = new FakePedidoRepository();
        var eventos = new FakePublicadorEventos();
        var handler = new CrearPedidoHandler(repositorio, eventos);

        var id = await handler.Handle(CommandValido(), CancellationToken.None);

        Assert.Single(repositorio.Pedidos);
        Assert.Equal(1, repositorio.VecesGuardado);
        Assert.Contains(repositorio.Pedidos, p => p.Id == id);

        var publicado = Assert.Single(eventos.Publicados);
        var evento = Assert.IsType<PedidoCreado>(publicado);
        Assert.Equal(id, evento.PedidoId);
    }

    [Fact]
    public async Task ConfirmarPedido_Existente_DevuelveTrueYConfirma()
    {
        var repositorio = new FakePedidoRepository();
        var pedido = Pedido.Crear(Guid.NewGuid(), new List<LineaPedido> {new(Guid.NewGuid(), cantidad: 2, precioUnitario: 10m)});
        repositorio.Pedidos.Add(pedido);
        var handler = new ConfirmarPedidoHandler(repositorio);

        var resultado = await handler.Handle(new ConfirmarPedidoCommand(pedido.Id), CancellationToken.None);

        Assert.True(resultado);
        Assert.Equal(EstadoPedido.Confirmado, pedido.Estado);
        Assert.Equal(1, repositorio.VecesGuardado);
    }

    [Fact]
    public async Task ConfirmarPedido_Inexistente_DevuelveFalseYNoGuarda()
    {
        var repositorio = new FakePedidoRepository();
        var handler = new ConfirmarPedidoHandler(repositorio);

        var resultado = await handler.Handle(new ConfirmarPedidoCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(resultado);
        Assert.Equal(0, repositorio.VecesGuardado);
    }

     [Fact]
    public async Task CancelarPedido_Existente_DevuelveTrueYConfirma()
    {
        var repositorio = new FakePedidoRepository();
        var pedido = Pedido.Crear(Guid.NewGuid(), new List<LineaPedido> {new(Guid.NewGuid(), cantidad: 2, precioUnitario: 10m)});
        repositorio.Pedidos.Add(pedido);
        var handler = new CancelarPedidoHandler(repositorio);

        var resultado = await handler.Handle(new CancelarPedidoCommand(pedido.Id), CancellationToken.None);

        Assert.True(resultado);
        Assert.Equal(EstadoPedido.Cancelado, pedido.Estado);
        Assert.Equal(1, repositorio.VecesGuardado);
    }

    [Fact]
    public async Task CancelarPedido_Inexistente_DevuelveFalseYNoGuarda()
    {
        var repositorio = new FakePedidoRepository();
        var handler = new CancelarPedidoHandler(repositorio);

        var resultado = await handler.Handle(new CancelarPedidoCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(resultado);
        Assert.Equal(0, repositorio.VecesGuardado);
    }
}