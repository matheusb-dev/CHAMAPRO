namespace Chamapro.Api.Models.Entities;

public class Categoria
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Icone { get; internal set; }
    public bool Ativa { get; internal set; }
}
