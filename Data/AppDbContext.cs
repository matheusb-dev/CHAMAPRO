using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chamapro.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Profissional> Profissionais => Set<Profissional>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoDestinatario> PedidoDestinatarios => Set<PedidoDestinatario>();
    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<Avaliacao> Avaliacoes => Set<Avaliacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
