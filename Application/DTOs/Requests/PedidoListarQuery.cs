using System.ComponentModel.DataAnnotations;
using Chamapro.Api.Models.Enums;

namespace Chamapro.Api.DTOs.Requests;

public class PedidoListarQuery : IValidatableObject
{
    /// <summary>Filtro opcional, como no JSON: PENDENTE, ACEITO, AGENDADO, EM_ANDAMENTO, CONCLUIDO ou CANCELADO.</summary>
    public string? Status { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A página deve ser 1 ou maior.")]
    public int Pagina { get; set; } = 1;

    [Range(1, 50, ErrorMessage = "O tamanho deve ficar entre 1 e 50.")]
    public int Tamanho { get; set; } = 10;

    public StatusPedido? ObterStatus()
        => EnumTexto.TryParse<StatusPedido>(Status, out var s) ? s : null;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(Status) && !EnumTexto.TryParse<StatusPedido>(Status, out _))
            yield return new ValidationResult("Status inválido.", [nameof(Status)]);
    }
}
