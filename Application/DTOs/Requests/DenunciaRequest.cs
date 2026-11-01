namespace Chamapro.Api.DTOs.Requests;

public class DenunciaRequest
{
    public int DenunciadoId { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
}