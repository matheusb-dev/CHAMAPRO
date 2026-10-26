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

        modelBuilder.Entity<Usuario>().HasKey(x => x.Id);
        modelBuilder.Entity<Categoria>().HasKey(x => x.Id);
        modelBuilder.Entity<Profissional>().HasKey(x => x.Id);
        modelBuilder.Entity<AreaAtendimento>().HasKey(x => x.Id);
        modelBuilder.Entity<Pedido>().HasKey(x => x.Id);
        modelBuilder.Entity<Orcamento>().HasKey(x => x.Id);
        modelBuilder.Entity<Agendamento>().HasKey(x => x.Id);
        modelBuilder.Entity<Avaliacao>().HasKey(x => x.Id);
        modelBuilder.Entity<VerificacaoProfissional>().HasKey(x => x.Id);

        modelBuilder.Entity<ProfissionalCategoria>()
            .HasKey(x => new { x.ProfissionalId, x.CategoriaId });

        modelBuilder.Entity<PedidoDestinatario>()
            .HasKey(x => new { x.PedidoId, x.ProfissionalId });

        modelBuilder.Entity<Pedido>()
            .HasOne(x => x.Cliente)
            .WithMany()
            .HasForeignKey(x => x.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Pedido>()
            .HasOne(x => x.Categoria)
            .WithMany()
            .HasForeignKey(x => x.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Pedido>()
            .HasOne(x => x.Profissional)
            .WithMany()
            .HasForeignKey(x => x.ProfissionalId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PedidoDestinatario>()
            .HasOne(x => x.Pedido)
            .WithMany(x => x.Destinatarios)
            .HasForeignKey(x => x.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PedidoDestinatario>()
            .HasOne(x => x.Profissional)
            .WithMany()
            .HasForeignKey(x => x.ProfissionalId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProfissionalCategoria>()
            .HasOne(x => x.Profissional)
            .WithMany(x => x.Categorias)
            .HasForeignKey(x => x.ProfissionalId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProfissionalCategoria>()
            .HasOne(x => x.Categoria)
            .WithMany()
            .HasForeignKey(x => x.CategoriaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VerificacaoProfissional>()
            .HasOne(x => x.Profissional)
            .WithMany()
            .HasForeignKey(x => x.ProfissionalId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
