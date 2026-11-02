
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
    public DbSet<Denuncia> Denuncias => Set<Denuncia>();
    public DbSet<Favorito> Favoritos => Set<Favorito>();
    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();
    public DbSet<DisponibilidadeProfissional> DisponibilidadesProfissionais => Set<DisponibilidadeProfissional>();
    public DbSet<ArquivoPrivado> ArquivosPrivados => Set<ArquivoPrivado>();
    public DbSet<HistoricoPedido> HistoricosPedidos => Set<HistoricoPedido>();
    public DbSet<AuditoriaAdmin> AuditoriasAdmin => Set<AuditoriaAdmin>();
    public DbSet<RecuperacaoSenha> RecuperacoesSenha => Set<RecuperacaoSenha>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tabelas
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
        modelBuilder.Entity<Denuncia>().ToTable("denuncias");
        modelBuilder.Entity<Favorito>().ToTable("favoritos");
        modelBuilder.Entity<Notificacao>().ToTable("notificacoes");
        modelBuilder.Entity<DisponibilidadeProfissional>().ToTable("disponibilidade_profissional");
        modelBuilder.Entity<ArquivoPrivado>().ToTable("arquivos_privados");
        modelBuilder.Entity<HistoricoPedido>().ToTable("historico_pedidos");
        modelBuilder.Entity<AuditoriaAdmin>().ToTable("auditoria_admin");
        modelBuilder.Entity<RecuperacaoSenha>().ToTable("recuperacoes_senha");

        // Enums do PostgreSQL
        modelBuilder.HasPostgresEnum<TipoUsuario>(
            name: "tipo_usuario",
            nameTranslator: new NpgsqlNullNameTranslator());

        modelBuilder.HasPostgresEnum<StatusPedido>(
            name: "status_pedido",
            nameTranslator: new NpgsqlNullNameTranslator());

        modelBuilder.HasPostgresEnum<StatusAgendamento>(
            name: "status_agendamento",
            nameTranslator: new NpgsqlNullNameTranslator());

        modelBuilder.HasPostgresEnum<StatusVerificacao>(
            name: "status_verificacao",
            nameTranslator: new NpgsqlNullNameTranslator());

        // Chaves primárias
        modelBuilder.Entity<Usuario>().HasKey(x => x.Id);
        modelBuilder.Entity<Categoria>().HasKey(x => x.Id);
        modelBuilder.Entity<Profissional>().HasKey(x => x.Id);
        modelBuilder.Entity<AreaAtendimento>().HasKey(x => x.Id);
        modelBuilder.Entity<Pedido>().HasKey(x => x.Id);
        modelBuilder.Entity<Orcamento>().HasKey(x => x.Id);
        modelBuilder.Entity<Agendamento>().HasKey(x => x.Id);
        modelBuilder.Entity<Avaliacao>().HasKey(x => x.Id);
        modelBuilder.Entity<VerificacaoProfissional>().HasKey(x => x.Id);
        modelBuilder.Entity<Denuncia>().HasKey(x => x.Id);
        modelBuilder.Entity<Favorito>().HasKey(x => new { x.ClienteId, x.ProfissionalId });
        modelBuilder.Entity<Notificacao>().HasKey(x => x.Id);
        modelBuilder.Entity<DisponibilidadeProfissional>()
            .HasKey(x => new { x.ProfissionalId, x.DiaSemana, x.HoraInicio });
        modelBuilder.Entity<ArquivoPrivado>().HasKey(x => x.NomeArquivo);
        modelBuilder.Entity<HistoricoPedido>().HasKey(x => x.Id);
        modelBuilder.Entity<AuditoriaAdmin>().HasKey(x => x.Id);

        // Configuração de Profissional
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

        // Auditoria administrativa
        modelBuilder.Entity<AuditoriaAdmin>()
            .HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(x => x.AdminId)
            .OnDelete(DeleteBehavior.Restrict);

        // Disponibilidade do profissional
        modelBuilder.Entity<DisponibilidadeProfissional>()
            .HasOne(x => x.Profissional)
            .WithMany(p => p.Disponibilidade)
            .HasForeignKey(x => x.ProfissionalId)
            .OnDelete(DeleteBehavior.Cascade);

        // Notificações
        modelBuilder.Entity<Notificacao>()
            .Property(x => x.Dados)
            .HasColumnType("jsonb");

        modelBuilder.Entity<Notificacao>()
            .HasOne(x => x.Usuario)
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        // Favoritos
        modelBuilder.Entity<Favorito>()
            .HasOne(x => x.Cliente)
            .WithMany()
            .HasForeignKey(x => x.ClienteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Favorito>()
            .HasOne(x => x.Profissional)
            .WithMany()
            .HasForeignKey(x => x.ProfissionalId)
            .OnDelete(DeleteBehavior.Cascade);

        // Arquivos privados
        modelBuilder.Entity<ArquivoPrivado>()
            .HasOne(x => x.Usuario)
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        // Histórico de pedidos
        modelBuilder.Entity<HistoricoPedido>()
            .HasOne<Pedido>()
            .WithMany()
            .HasForeignKey(x => x.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HistoricoPedido>()
            .HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(x => x.AtorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Conversões de propriedades
        modelBuilder.Entity<Pedido>()
            .Property(x => x.TipoAtendimento)
            .HasConversion<string>();

        modelBuilder.Entity<PedidoDestinatario>()
            .Property(x => x.Situacao)
            .HasConversion<string>();

        modelBuilder.Entity<Orcamento>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Denuncia>()
            .Property(x => x.Status)
            .HasConversion<string>();

        // Chaves compostas
        modelBuilder.Entity<ProfissionalCategoria>()
            .HasKey(x => new { x.ProfissionalId, x.CategoriaId });

        modelBuilder.Entity<PedidoDestinatario>()
            .HasKey(x => new { x.PedidoId, x.ProfissionalId });

        // Relacionamentos de Pedido
        modelBuilder.Entity<Profissional>()
            .HasIndex(x => x.UsuarioId).IsUnique();

        modelBuilder.Entity<Pedido>()
            .HasOne<Usuario>().WithMany()
            .HasForeignKey(x => x.CanceladoPorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Agendamento>()
            .HasOne<Profissional>().WithMany()
            .HasForeignKey(x => x.ProfissionalId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Avaliacao>()
            .HasOne<Usuario>().WithMany()
            .HasForeignKey(x => x.AvaliadorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Avaliacao>()
            .HasOne<Usuario>().WithMany()
            .HasForeignKey(x => x.AvaliadoId)
            .OnDelete(DeleteBehavior.Restrict);

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

        // Destinatários do pedido
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

        // Categorias dos profissionais
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

        // Verificação de profissionais
        modelBuilder.Entity<VerificacaoProfissional>()
            .HasOne(x => x.Profissional)
            .WithMany()
            .HasForeignKey(x => x.ProfissionalId)
            .OnDelete(DeleteBehavior.Cascade);

        // Orçamentos
        modelBuilder.Entity<Orcamento>()
            .HasOne(x => x.Pedido)
            .WithMany(x => x.Orcamentos)
            .HasForeignKey(x => x.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Orcamento>()
            .HasOne(x => x.Profissional)
            .WithMany()
            .HasForeignKey(x => x.ProfissionalId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Orcamento>()
            .Property(x => x.Valor)
            .HasPrecision(10, 2);

        // Agendamento: relacionamento um para um com Pedido.
        // PedidoId é a única chave estrangeira desse relacionamento.
        modelBuilder.Entity<Agendamento>()
            .HasOne(x => x.Pedido)
            .WithOne(x => x.Agendamento)
            .HasForeignKey<Agendamento>(x => x.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Agendamento>()
            .HasIndex(x => new { x.ProfissionalId, x.DataHora })
            .IsUnique()
            .HasFilter("status <> 'Cancelado' AND profissional_id IS NOT NULL");

        // Avaliação: relacionamento um para um com Pedido.
        modelBuilder.Entity<Avaliacao>()
            .HasOne<Pedido>()
            .WithOne(x => x.Avaliacao)
            .HasForeignKey<Avaliacao>(x => x.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Denúncias
        modelBuilder.Entity<Denuncia>()
            .HasOne(x => x.Denunciante)
            .WithMany()
            .HasForeignKey(x => x.DenuncianteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Denuncia>()
            .HasOne(x => x.Denunciado)
            .WithMany()
            .HasForeignKey(x => x.DenunciadoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Padronização dos nomes das colunas para snake_case
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ParaSnakeCase(property.Name));
            }
        }

        modelBuilder.Entity<Categoria>()
            .Property(x => x.Icone)
            .HasColumnName("icone");

        modelBuilder.Entity<PedidoDestinatario>()
            .Property(x => x.Situacao)
            .HasColumnName("status");

        // Configurações adicionais
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    private static string ParaSnakeCase(string name)
    {
        var resultado = new System.Text.StringBuilder(name.Length + 8);

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (char.IsUpper(c) &&
                i > 0 &&
                (char.IsLower(name[i - 1]) ||
                 (i + 1 < name.Length && char.IsLower(name[i + 1]))))
            {
                resultado.Append('_');
            }

            resultado.Append(char.ToLowerInvariant(c));
        }

        return resultado.ToString();
    }
}
