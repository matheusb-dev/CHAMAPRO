using Chamapro.Api.Models.Enums;

namespace Chamapro.Api.Models.Entities;

public class Denuncia
{
    public int Id { get; set; }

    public int DenuncianteId { get; set; }
    public Usuario Denunciante { get; set; } = null!;

    public int DenunciadoId { get; set; }
    public Usuario Denunciado { get; set; } = null!;

    public string TipoEntidade { get; set; } = string.Empty;
    public int EntidadeId { get; set; }

    public string Motivo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? Evidencias { get; set; }

    public StatusDenuncia Status { get; set; } = StatusDenuncia.Pendente;
    public string? ObservacaoAdmin { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }
}