using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica todas as classes IEntityTypeConfiguration<T> do projeto
        // (inclui Data/Configurations/PedidosModuloConfiguration.cs: Pedido, PedidoDestinatario e Orcamento).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // TODO(P1): mapeamento das demais Entities e dos enums do PostgreSQL.
    }
}