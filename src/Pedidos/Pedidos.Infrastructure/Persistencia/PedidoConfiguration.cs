using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pedidos.Domain.Pedidos;

namespace Pedidos.Infrastructure.Persistencia;

public class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20);

        builder.OwnsMany(p => p.Lineas, linea =>
        {
            linea.ToTable("LineasPedido");
            linea.WithOwner().HasForeignKey("PedidoId");
            linea.Property<int>("Id");
            linea.HasKey("Id");
            linea.Property(l => l.PrecioUnitario).HasPrecision(18, 2);
        });

        builder.Navigation(p => p.Lineas)
               .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}