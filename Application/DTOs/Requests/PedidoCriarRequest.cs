using System.ComponentModel.DataAnnotations;
using Chamapro.Api.Models.Enums;

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

    [StringLength(250, ErrorMessage = "O endereço deve ter até 250 caracteres.")]
    public string? Endereco { get; set; }

    public TipoAtendimento TipoAtendimento { get; set; } = TipoAtendimento.Imediato;

    public DateTime? DataDesejada { get; set; }

    /// <summary>Quando true, direciona o pedido a até cinco profissionais verificados compatíveis da região.</summary>
    public bool DistribuirParaCompativeis { get; set; }

    /// <summary>URLs obtidas em POST /arquivos (até 5).</summary>
    public List<string> FotoUrls { get; set; } = new();

    /// <summary>De 1 a 5 profissionais selecionados individualmente (RN04).</summary>
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

        if (DistribuirParaCompativeis && ProfissionalIds.Count > 0)
            yield return new ValidationResult("Escolha profissionais ou distribua o chamado, não os dois.", [nameof(ProfissionalIds)]);

        if (!DistribuirParaCompativeis && ProfissionalIds.Count == 0)
            yield return new ValidationResult("Escolha profissionais ou ative a distribuição para compatíveis.", [nameof(ProfissionalIds)]);

        if (TipoAtendimento == TipoAtendimento.Agendado && DataDesejada is null)
            yield return new ValidationResult("Informe a data e o horário desejados.", [nameof(DataDesejada)]);

        if (TipoAtendimento == TipoAtendimento.Agendado && DataDesejada <= DateTime.UtcNow)
            yield return new ValidationResult("A data desejada precisa estar no futuro.", [nameof(DataDesejada)]);
    }
}
