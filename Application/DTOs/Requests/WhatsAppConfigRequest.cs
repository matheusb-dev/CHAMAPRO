using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public class WhatsAppConfigRequest
{
    [Required]
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Informe DDD e telefone com 10 ou 11 dígitos.")]
    public string Telefone { get; set; } = string.Empty;

    public bool WhatsAppAtivo { get; set; } = true;

    [StringLength(300)]
    public string? MensagemInicialWhatsApp { get; set; }
}
