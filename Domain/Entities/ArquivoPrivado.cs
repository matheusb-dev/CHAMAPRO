namespace Chamapro.Api.Models.Entities;

public sealed class ArquivoPrivado
{
    public string NomeArquivo { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public Usuario Usuario { get; set; } = null!;
}
