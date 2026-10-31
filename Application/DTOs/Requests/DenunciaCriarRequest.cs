namespace Chamapro.Api.DTOs.Requests;

public class DenunciaCriarRequest
{
    public int DenunciadoId { get; set; }
    public string TipoEntidade { get; set; } = string.Empty;
    public int EntidadeId { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? Evidencias { get; set; }
}