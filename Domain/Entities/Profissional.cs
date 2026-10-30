namespace Chamapro.Api.Models.Entities;

public class Profissional
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string DescricaoPerfil { get; set; } = string.Empty;
    public decimal? PrecoMin { get; set; }
    public decimal? PrecoMax { get; set; }
    public decimal MediaAvaliacao { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public ICollection<ProfissionalCategoria> Categorias { get; set; } = new List<ProfissionalCategoria>();
    public ICollection<AreaAtendimento> Areas { get; set; } = new List<AreaAtendimento>();
}
