namespace Chamapro.Api.Models.Entities;

public class Categoria
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Icone { get; set; }
    public bool Ativa { get; set; } = true;
}
