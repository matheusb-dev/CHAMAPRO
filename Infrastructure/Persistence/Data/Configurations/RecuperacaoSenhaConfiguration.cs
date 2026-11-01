using Chamapro.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chamapro.Api.Data.Configurations;

public class RecuperacaoSenhaConfiguration : IEntityTypeConfiguration<RecuperacaoSenha>
{
    public void Configure(EntityTypeBuilder<RecuperacaoSenha> b)
    {
        b.ToTable("recuperacoes_senha");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        b.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
        b.Property(x => x.ExpiraEm).HasColumnName("expira_em").IsRequired();
        b.Property(x => x.UsadoEm).HasColumnName("usado_em");
        b.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("ux_recuperacoes_senha_token_hash");
        b.HasIndex(x => new { x.UsuarioId, x.ExpiraEm }).HasDatabaseName("idx_recuperacoes_senha_usuario_expira");
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}
