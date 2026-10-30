using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public class AreaAtendimentoRequest
{
    [Required(ErrorMessage = "A cidade é obrigatória.")]
    [StringLength(100, ErrorMessage = "A cidade deve ter no máximo 100 caracteres.")]
    public string Cidade { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "O bairro deve ter no máximo 100 caracteres.")]
    public string? Bairro { get; set; }
}