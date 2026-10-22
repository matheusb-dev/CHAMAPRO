using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Data;

/// <summary>
/// Contexto do EF Core. Está vazio de propósito: a Pessoa 1 gera as Entities e preenche os DbSets
/// e o OnModelCreating a partir do script chamapro_schema_v2.sql.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // TODO(P1): mapeamento das Entities e dos enums do PostgreSQL.
    }
}
