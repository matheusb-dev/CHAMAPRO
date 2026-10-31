using Microsoft.EntityFrameworkCore;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Npgsql;
using Npgsql.NameTranslation;

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

        modelBuilder.Entity<Usuario>().ToTable("usuarios");
        modelBuilder.Entity<Categoria>().ToTable("categorias");
        modelBuilder.Entity<Profissional>().ToTable("profissionais");
        modelBuilder.Entity<ProfissionalCategoria>().ToTable("profissional_categorias");
        modelBuilder.Entity<AreaAtendimento>().ToTable("areas_atendimento");
        modelBuilder.Entity<Pedido>().ToTable("pedidos");
        modelBuilder.Entity<PedidoDestinatario>().ToTable("pedido_destinatarios");
        modelBuilder.Entity<Orcamento>().ToTable("orcamentos");
        modelBuilder.Entity<Agendamento>().ToTable("agendamentos");
        modelBuilder.Entity<Avaliacao>().ToTable("avaliacoes");
        modelBuilder.Entity<VerificacaoProfissional>().ToTable("verificacoes_profissionais");

        modelBuilder.HasPostgresEnum<TipoUsuario>(name: "tipo_usuario", nameTranslator: new NpgsqlNullNameTranslator());

        modelBuilder.Entity<Usuario>().HasKey(x => x.Id);
        modelBuilder.Entity<Categoria>().HasKey(x => x.Id);
        modelBuilder.Entity<Profissional>().HasKey(x => x.Id);
        modelBuilder.Entity<Profissional>(entity =>
        {
            entity.ToTable("profissionais", "public");

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UsuarioId).HasColumnName("usuario_id");
            entity.Property(x => x.DescricaoPerfil).HasColumnName("descricao_perfil");
            entity.Property(x => x.PrecoMin).HasColumnName("preco_min");
            entity.Property(x => x.PrecoMax).HasColumnName("preco_max");
            entity.Property(x => x.MediaAvaliacao).HasColumnName("media_avaliacao");
        });
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

        modelBuilder.Entity<Orcamento>()
            .HasOne<Pedido>()
            .WithMany()
            .HasForeignKey(x => x.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Orcamento>()
            .HasOne<Profissional>()
            .WithMany()
            .HasForeignKey(x => x.ProfissionalId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Orcamento>()
            .Property(x => x.Valor)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Agendamento>()
            .HasOne<Pedido>()
            .WithMany()
            .HasForeignKey(x => x.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Avaliacao>()
            .HasOne<Pedido>()
            .WithMany()
            .HasForeignKey(x => x.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}