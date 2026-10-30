using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public class VerificacaoDocumentosRequest
{
    [Required(ErrorMessage = "Informe o tipo de documento.")]
    public string TipoDocumento { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a URL do documento.")]
    [Url(ErrorMessage = "A URL do documento é inválida.")]
    public string UrlDocumento { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "A observação deve ter no máximo 500 caracteres.")]
    public string? Observacao { get; set; }
}
