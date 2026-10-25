using Microsoft.EntityFrameworkCore;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;

namespace Chamapro.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Profissional> Profissionais => Set<Profissional>();
    public DbSet<ProfissionalCategoria> ProfissionalCategorias => Set<ProfissionalCategoria>();
    public DbSet<AreaAtendimento> AreasAtendimento => Set<AreaAtendimento>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoDestinatario> PedidoDestinatarios => Set<PedidoDestinatario>();
    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<Avaliacao> Avaliacoes => Set<Avaliacao>();
    public DbSet<VerificacaoProfissional> Verificacoes => Set<VerificacaoProfissional>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
