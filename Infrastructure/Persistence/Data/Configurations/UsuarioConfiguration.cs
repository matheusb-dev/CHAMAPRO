using Chamapro.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chamapro.Api.Data.Configurations;

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
        b.Property(u => u.Tipo).HasColumnName("tipo");
        b.Property(u => u.IdentidadeVerificada).HasColumnName("identidade_verificada");
        b.Property(u => u.Ativo).HasColumnName("ativo").HasDefaultValue(true);
        b.Property(u => u.CriadoEm).HasColumnName("criado_em");

        b.HasIndex(u => u.Email).IsUnique();
    }
}
