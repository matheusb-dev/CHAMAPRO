namespace Chamapro.Api.Models.Entities;

public sealed class AuditoriaAdmin
{
    public long Id { get; set; }
    public int AdminId { get; set; }
    public string Acao { get; set; } = string.Empty;
    public string Entidade { get; set; } = string.Empty;
    public int EntidadeId { get; set; }
    public string? EstadoAnterior { get; set; }
    public string? EstadoNovo { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
