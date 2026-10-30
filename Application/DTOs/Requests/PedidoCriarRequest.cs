using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public class PedidoCriarRequest : IValidatableObject
{
    private const int MaxFotos = 5;

    [Range(1, int.MaxValue, ErrorMessage = "Informe a categoria do serviço.")]
    public int CategoriaId { get; set; }

    [Required(ErrorMessage = "O título é obrigatório.")]
    [StringLength(80, MinimumLength = 5, ErrorMessage = "O título deve ter de 5 a 80 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "A descrição deve ter de 10 a 1000 caracteres.")]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "A cidade é obrigatória.")]
    [StringLength(100, ErrorMessage = "A cidade deve ter até 100 caracteres.")]
    public string Cidade { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "O bairro deve ter até 100 caracteres.")]
    public string? Bairro { get; set; }

    /// <summary>URLs obtidas em POST /arquivos (até 5).</summary>
    public List<string> FotoUrls { get; set; } = new();

    /// <summary>De 1 a 5 profissionais que vão receber o pedido (RN04).</summary>
    [MinLength(1, ErrorMessage = "Escolha pelo menos 1 profissional.")]
    [MaxLength(5, ErrorMessage = "Escolha no máximo 5 profissionais.")]
    public List<int> ProfissionalIds { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FotoUrls.Count > MaxFotos)
            yield return new ValidationResult($"Envie no máximo {MaxFotos} fotos.", [nameof(FotoUrls)]);

        if (FotoUrls.Any(f => string.IsNullOrWhiteSpace(f) || f.Length > 500))
            yield return new ValidationResult("Cada foto deve ser uma URL de até 500 caracteres.", [nameof(FotoUrls)]);

        if (ProfissionalIds.Any(id => id <= 0))
            yield return new ValidationResult("Identificador de profissional inválido.", [nameof(ProfissionalIds)]);

        if (ProfissionalIds.Distinct().Count() != ProfissionalIds.Count)
            yield return new ValidationResult("Não repita o mesmo profissional.", [nameof(ProfissionalIds)]);
    }
}
