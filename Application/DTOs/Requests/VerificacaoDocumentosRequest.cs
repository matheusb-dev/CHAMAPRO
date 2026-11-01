using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public class VerificacaoDocumentosRequest
{
    [Required]
    public string TipoDocumento { get; set; } = string.Empty;

    [Required]
    public string UrlDocumento { get; set; } = string.Empty;

    public string? Observacao { get; set; }
}