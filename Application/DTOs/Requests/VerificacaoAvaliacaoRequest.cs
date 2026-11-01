using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public class VerificacaoAvaliacaoRequest
    : IValidatableObject
{
    public bool? Aprovado { get; set; }

    public string? Status { get; set; }

    [StringLength(500, ErrorMessage = "A observação deve ter no máximo 500 caracteres.")]
    public string? Observacao { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Aprovado is null && string.IsNullOrWhiteSpace(Status))
            yield return new ValidationResult("Informe a decisão ou o status.", [nameof(Status)]);
    }
}
