using Chamapro.Api.Models.Enums;

namespace Chamapro.Api.Models.Entities;

public class VerificacaoProfissional
{
    public int Id { get; set; }
    public int ProfissionalId { get; set; }
    public string TipoDocumento { get; set; } = string.Empty;
    public string UrlDocumento { get; set; } = string.Empty;
    public StatusVerificacao Status { get; set; } = StatusVerificacao.Pendente;
    public string? Observacao { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Profissional Profissional { get; set; } = null!;
}