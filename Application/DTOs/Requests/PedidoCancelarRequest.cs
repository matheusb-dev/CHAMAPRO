using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public class PedidoCancelarRequest
{
    [Required(ErrorMessage = "O motivo é obrigatório.")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "O motivo deve ter de 5 a 200 caracteres.")]
    public string Motivo { get; set; } = string.Empty;
}
