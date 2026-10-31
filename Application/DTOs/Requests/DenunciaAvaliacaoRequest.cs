namespace Chamapro.Api.DTOs.Requests;

public class DenunciaAvaliacaoRequest
{
    public string Status { get; set; } = "EmAnalise";
    public string? Observacao { get; set; }
}