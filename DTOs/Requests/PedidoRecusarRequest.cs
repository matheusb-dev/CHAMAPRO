using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public class PedidoRecusarRequest
{
    [StringLength(200, ErrorMessage = "O motivo deve ter até 200 caracteres.")]
    public string? Motivo { get; set; }
}
