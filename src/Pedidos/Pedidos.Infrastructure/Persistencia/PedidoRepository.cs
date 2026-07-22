using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Abstracciones;
using Pedidos.Domain.Pedidos;

namespace Pedidos.Infrastructure.Persistencia;

public class PedidoRepository : IPedidoRepository
{
    private readonly PedidosDbContext _context;

    public PedidoRepository(PedidosDbContext context)
    {
        _context = context;
    }

    public async Task<Pedido?> ObtenerPorId(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task Agregar(Pedido pedido, CancellationToken cancellationToken)
    {
        await _context.AddAsync(pedido, cancellationToken);
    }

    public async Task GuardarCambios(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}