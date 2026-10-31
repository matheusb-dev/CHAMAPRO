namespace Chamapro.Api.DTOs.Responses;

public class DenunciaResponse
{
    public int Id { get; set; }
    public int DenuncianteId { get; set; }
    public int DenunciadoId { get; set; }
    public string TipoEntidade { get; set; } = string.Empty;
    public int EntidadeId { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? Evidencias { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ObservacaoAdmin { get; set; }
    public DateTime CriadoEm { get; set; }
}