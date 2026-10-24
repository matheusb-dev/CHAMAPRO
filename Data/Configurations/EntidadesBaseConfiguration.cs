using Chamapro.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chamapro.Api.Data.Configurations;

// Aplicadas por modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly).

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuarios");
        b.HasKey(u => u.Id);

        b.Property(u => u.Id).HasColumnName("id");
        b.Property(u => u.Nome).HasColumnName("nome").HasMaxLength(100);
        b.Property(u => u.Email).HasColumnName("email").HasMaxLength(120);
        b.Property(u => u.SenhaHash).HasColumnName("senha_hash").HasMaxLength(255);
        b.Property(u => u.Tipo).HasColumnName("tipo");                       // enum nativo tipo_usuario
        b.Property(u => u.IdentidadeVerificada).HasColumnName("identidade_verificada");
        b.Property(u => u.CriadoEm).HasColumnName("criado_em");

        b.HasIndex(u => u.Email).IsUnique();
    }
}

public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> b)
    {
        b.ToTable("categorias");
        b.HasKey(c => c.Id);

        b.Property(c => c.Id).HasColumnName("id");
        b.Property(c => c.Nome).HasColumnName("nome").HasMaxLength(80);
        b.Property(c => c.Descricao).HasColumnName("descricao");

        b.HasIndex(c => c.Nome).IsUnique();
    }
}

public class ProfissionalConfiguration : IEntityTypeConfiguration<Profissional>
{
    public void Configure(EntityTypeBuilder<Profissional> b)
    {
        b.ToTable("profissionais");
        b.HasKey(p => p.Id);

        b.Property(p => p.Id).HasColumnName("id");
        b.Property(p => p.UsuarioId).HasColumnName("usuario_id");
        b.Property(p => p.DescricaoPerfil).HasColumnName("descricao_perfil");
        b.Property(p => p.PrecoMin).HasColumnName("preco_min").HasPrecision(10, 2);
        b.Property(p => p.PrecoMax).HasColumnName("preco_max").HasPrecision(10, 2);
        b.Property(p => p.MediaAvaliacao).HasColumnName("media_avaliacao").HasPrecision(2, 1);

        b.HasIndex(p => p.UsuarioId).IsUnique();
        b.HasOne(p => p.Usuario).WithMany().HasForeignKey(p => p.UsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProfissionalCategoriaConfiguration : IEntityTypeConfiguration<ProfissionalCategoria>
{
    public void Configure(EntityTypeBuilder<ProfissionalCategoria> b)
    {
        b.ToTable("profissional_categorias");
        b.HasKey(pc => new { pc.ProfissionalId, pc.CategoriaId });

        b.Property(pc => pc.ProfissionalId).HasColumnName("profissional_id");
        b.Property(pc => pc.CategoriaId).HasColumnName("categoria_id");

        b.HasOne(pc => pc.Profissional).WithMany(p => p.Categorias).HasForeignKey(pc => pc.ProfissionalId);
        b.HasOne(pc => pc.Categoria).WithMany().HasForeignKey(pc => pc.CategoriaId);
    }
}

public class AreaAtendimentoConfiguration : IEntityTypeConfiguration<AreaAtendimento>
{
    public void Configure(EntityTypeBuilder<AreaAtendimento> b)
    {
        b.ToTable("areas_atendimento");
        b.HasKey(a => a.Id);

        b.Property(a => a.Id).HasColumnName("id");
        b.Property(a => a.ProfissionalId).HasColumnName("profissional_id");
        b.Property(a => a.Cidade).HasColumnName("cidade").HasMaxLength(100);
        b.Property(a => a.Bairro).HasColumnName("bairro").HasMaxLength(100);

        b.HasOne(a => a.Profissional).WithMany(p => p.Areas).HasForeignKey(a => a.ProfissionalId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AgendamentoConfiguration : IEntityTypeConfiguration<Agendamento>
{
    public void Configure(EntityTypeBuilder<Agendamento> b)
    {
        b.ToTable("agendamentos");
        b.HasKey(a => a.Id);

        b.Property(a => a.Id).HasColumnName("id");
        b.Property(a => a.PedidoId).HasColumnName("pedido_id");
        b.Property(a => a.DataHora).HasColumnName("data_hora");
        b.Property(a => a.Status).HasColumnName("status");                   // enum nativo status_agendamento
    }
}

public class AvaliacaoConfiguration : IEntityTypeConfiguration<Avaliacao>
{
    public void Configure(EntityTypeBuilder<Avaliacao> b)
    {
        b.ToTable("avaliacoes");
        b.HasKey(a => a.Id);

        b.Property(a => a.Id).HasColumnName("id");
        b.Property(a => a.PedidoId).HasColumnName("pedido_id");
        b.Property(a => a.AvaliadorId).HasColumnName("avaliador_id");
        b.Property(a => a.AvaliadoId).HasColumnName("avaliado_id");
        b.Property(a => a.Nota).HasColumnName("nota");
        b.Property(a => a.Comentario).HasColumnName("comentario");
    }
}
