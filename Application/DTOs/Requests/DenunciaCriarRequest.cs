using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public class DenunciaCriarRequest
{
    [Range(1, int.MaxValue)]
    public int? DenunciadoId { get; set; }

    [Required, StringLength(40, MinimumLength = 4)]
    public string TipoEntidade { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int EntidadeId { get; set; }

    [Required, StringLength(120, MinimumLength = 3)]
    public string Motivo { get; set; } = string.Empty;

    [Required, StringLength(4000, MinimumLength = 10)]
    public string Descricao { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Evidencias { get; set; }
}
