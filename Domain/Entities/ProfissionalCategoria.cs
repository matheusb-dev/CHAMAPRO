namespace Chamapro.Api.Models.Entities;

/// <summary>Categorias atendidas por um profissional (tabela associativa).</summary>
public class ProfissionalCategoria
{
    public int ProfissionalId { get; set; }
    public int CategoriaId { get; set; }

    public Profissional Profissional { get; set; } = null!;
    public Categoria Categoria { get; set; } = null!;
}
