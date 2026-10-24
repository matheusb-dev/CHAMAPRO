using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public class AtualizarClienteRequest
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O telefone é obrigatório.")]
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "O telefone deve ter 10 ou 11 dígitos, apenas números.")]
    public string Telefone { get; set; } = string.Empty;

    [Required(ErrorMessage = "A cidade é obrigatória.")]
    [StringLength(100, ErrorMessage = "A cidade deve ter no máximo 100 caracteres.")]
    public string Cidade { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "A URL da foto deve ter no máximo 300 caracteres.")]
    public string? FotoUrl { get; set; }
}