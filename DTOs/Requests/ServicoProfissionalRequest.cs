using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public class ServicoProfissionalRequest : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Informe uma categoria válida.")]
    public int CategoriaId { get; set; }

    [Range(0, 99999999.99, ErrorMessage = "A faixa de preço mínima deve ser maior ou igual a zero.")]
    public decimal FaixaPrecoMin { get; set; }

    [Range(0, 99999999.99, ErrorMessage = "A faixa de preço máxima deve ser maior ou igual a zero.")]
    public decimal FaixaPrecoMax { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext contexto)
    {
        if (FaixaPrecoMin > FaixaPrecoMax)
            yield return new ValidationResult(
                "A faixa de preço mínima não pode ser maior que a máxima.",
                new[] { nameof(FaixaPrecoMin) });
    }
}