namespace Chamapro.Api.DTOs.Responses;

public class VerificacaoResponse
{
    public int Id { get; set; }
    public int ProfissionalId { get; set; }
    public string TipoDocumento { get; set; } = string.Empty;
    public string UrlDocumento { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
    public string? Observacao { get; set; }
}
