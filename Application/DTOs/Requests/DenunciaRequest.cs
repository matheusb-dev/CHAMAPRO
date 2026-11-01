using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public class DenunciaRequest
{
    [Required]
    public int PedidoId { get; set; }

    [Required]
    public int DenunciadoId { get; set; }

    [Required]
    public string Motivo { get; set; } = string.Empty;

    [Required]
    public string Tipo { get; set; } = string.Empty;

    public string? Descricao { get; set; }
}