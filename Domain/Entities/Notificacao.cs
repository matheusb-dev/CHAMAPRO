namespace Chamapro.Api.Models.Entities;

public sealed class Notificacao
{
    public long Id { get; set; }
    public int UsuarioId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string Dados { get; set; } = "{}";
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    public DateTime? LidaEm { get; set; }
    public Usuario Usuario { get; set; } = null!;
}
