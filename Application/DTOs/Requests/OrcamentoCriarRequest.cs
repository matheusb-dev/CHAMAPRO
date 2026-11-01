using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Chamapro.Api.DTOs.Requests;

public class OrcamentoCriarRequest : IValidatableObject
{
    [Range(typeof(decimal), "0.01", "99999999.99", ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Valor { get; set; }

    [Required(ErrorMessage = "Informe o prazo estimado.")]
    [StringLength(100, ErrorMessage = "O prazo estimado deve ter até 100 caracteres.")]
    public string PrazoEstimado { get; set; } = string.Empty;

    /// <summary>O que está incluso, material, deslocamento.</summary>
    [StringLength(500, ErrorMessage = "As condições devem ter até 500 caracteres.")]
    public string? Condicoes { get; set; }

    /// <summary>Opcional. Sem valor, o orçamento vale por 7 dias.</summary>
    public DateTime? ValidoAte { get; set; }

    [JsonIgnore]
    public DateTime? ValidoAteUtc => ValidoAte is null ? null : ParaUtc(ValidoAte.Value);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ValidoAteUtc is { } validade && validade <= DateTime.UtcNow)
            yield return new ValidationResult("Deve ser uma data futura.", [nameof(ValidoAte)]);
    }

    private static DateTime ParaUtc(DateTime d) => d.Kind switch
    {
        DateTimeKind.Utc => d,
        DateTimeKind.Local => d.ToUniversalTime(),
        _ => DateTime.SpecifyKind(d, DateTimeKind.Utc)
    };
}
