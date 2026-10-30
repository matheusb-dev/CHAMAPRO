using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public class CadastroProfissionalRequest
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "A senha deve ter entre 8 e 128 caracteres.")]
    public string Senha { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "A descrição deve ter até 500 caracteres.")]
    public string? DescricaoPerfil { get; set; }
}
