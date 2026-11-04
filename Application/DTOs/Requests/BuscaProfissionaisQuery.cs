using System.ComponentModel.DataAnnotations;
using Chamapro.Api.Common;
using Chamapro.Api.Models.Enums;

namespace Chamapro.Api.DTOs.Requests;

public class BuscaProfissionaisQuery : PaginacaoQuery, IValidatableObject
{
    [Required(ErrorMessage = "A categoria é obrigatória.")]
    [Range(1, int.MaxValue, ErrorMessage = "Informe uma categoria válida.")]
    public int? CategoriaId { get; set; }

    [Required(ErrorMessage = "A cidade é obrigatória.")]
    [StringLength(100, ErrorMessage = "A cidade deve ter no máximo 100 caracteres.")]
    public string? Cidade { get; set; }

    [StringLength(100, ErrorMessage = "O bairro deve ter no máximo 100 caracteres.")]
    public string? Bairro { get; set; }

    public bool? Verificado { get; set; }

    [Range(1, 5, ErrorMessage = "A nota mínima deve estar entre 1 e 5.")]
    public int? NotaMinima { get; set; }

    [RegularExpression("^(nota|avaliacoes|preco)$", ErrorMessage = "Ordenação inválida. Use nota, avaliacoes ou preco.")]
    public string Ordenar { get; set; } = "nota";

    public TipoAtendimento? TipoAtendimento { get; set; }
    public DateTime? DataDesejada { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        yield break; // Legacy filter properties are accepted but no longer restrict hours.
    }
}
