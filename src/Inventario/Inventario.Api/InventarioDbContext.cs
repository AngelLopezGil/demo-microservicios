using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api;

public class InventarioDbContext : DbContext
{
    public InventarioDbContext(DbContextOptions<InventarioDbContext> options)
        : base(options) { }

    public DbSet<Stock> Stocks => Set<Stock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Stock>(e =>
        {
            e.ToTable("Stocks");
            e.HasKey(s => s.ProductoId);
        });

        modelBuilder.Entity<Stock>().HasData(
            new Stock { ProductoId = Guid.Parse("33333333-3333-3333-3333-333333333333"), Disponible = 10, Reservado = 0 },
            new Stock { ProductoId = Guid.Parse("44444444-4444-4444-4444-444444444444"), Disponible = 3,  Reservado = 0 },
            new Stock { ProductoId = Guid.Parse("55555555-5555-5555-5555-555555555555"), Disponible = 0,  Reservado = 0 });
        
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}