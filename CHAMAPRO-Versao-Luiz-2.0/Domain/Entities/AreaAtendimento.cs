namespace Chamapro.Api.Models.Entities;

/// <summary>Cidade (e bairro, se houver) em que o profissional atende. Tabela criada na mudança 002.</summary>
public class AreaAtendimento
{
    public int Id { get; set; }
    public int ProfissionalId { get; set; }
    public string Cidade { get; set; } = string.Empty;
    public string? Bairro { get; set; }

    public Profissional Profissional { get; set; } = null!;
}
