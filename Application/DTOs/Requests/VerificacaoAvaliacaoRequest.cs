using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public class VerificacaoAvaliacaoRequest
{
    [Required(ErrorMessage = "Informe a decisão.")]
    public bool Aprovado { get; set; }

    [StringLength(500, ErrorMessage = "A observação deve ter no máximo 500 caracteres.")]
    public string? Observacao { get; set; }
}
