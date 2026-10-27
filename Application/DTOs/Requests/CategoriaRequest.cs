using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public class CategoriaRequest
{
	[Required(ErrorMessage = "O nome é obrigatório.")]
	[StringLength(60, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 60 caracteres.")]
	public string Nome { get; set; } = string.Empty;

	[StringLength(60, ErrorMessage = "O ícone deve ter no máximo 60 caracteres.")]
	public string? Icone { get; set; }
}