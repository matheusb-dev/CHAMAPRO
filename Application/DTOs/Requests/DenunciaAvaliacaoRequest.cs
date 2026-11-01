using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public class DenunciaAvaliacaoRequest
{
    [Required]
    public string Status { get; set; } = "EmAnalise";

    [StringLength(2000)]
    public string? Observacao { get; set; }
}
