using Pedidos.Application.Abstracciones;
using Pedidos.Domain.Pedidos;

namespace Pedidos.Tests;

public class FakePedidoRepository : IPedidoRepository
{
    public List<Pedido> Pedidos { get; } = new();
    public int VecesGuardado { get; private set; }

    public Task<Pedido?> ObtenerPorId(Guid id, CancellationToken ct) =>
        Task.FromResult(Pedidos.FirstOrDefault(p => p.Id == id));

    public Task Agregar(Pedido pedido, CancellationToken ct)
    {
        Pedidos.Add(pedido);
        return Task.CompletedTask;
    }

    public Task GuardarCambios(CancellationToken ct)
    {
        VecesGuardado++;
        return Task.CompletedTask;
    }
}