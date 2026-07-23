using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Pedidos.ObtenerPedido;

namespace Pedidos.Infrastructure.Persistencia;

public class PedidoQueries : IPedidoQueries
{
    private readonly PedidosDbContext _context;

    public PedidoQueries(PedidosDbContext context)
    {
        _context = context;
    }

public async Task<PedidoDto?> ObtenerPorId(Guid id, CancellationToken cancellationToken)
{
    var datos = await _context.Pedidos
        .AsNoTracking()
        .Where(p => p.Id == id)
        .Select(p => new
        {
            p.Id,
            p.ClienteId,
            p.Estado,
            Total = p.Lineas.Sum(l => l.Cantidad * l.PrecioUnitario),
            Lineas = p.Lineas
                .Select(l => new LineaDto(l.ProductoId, l.Cantidad, l.PrecioUnitario))
                .ToList()
        })
        .FirstOrDefaultAsync(cancellationToken);

    return datos is null
        ? null
        : new PedidoDto(datos.Id, datos.ClienteId, datos.Estado.ToString(), datos.Total, datos.Lineas);
}
}