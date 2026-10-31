namespace Chamapro.Api.DTOs.Requests;

public class WhatsAppConfigRequest
{
    public string Telefone { get; set; } = string.Empty;
    public bool WhatsAppAtivo { get; set; } = true;
    public string? MensagemInicialWhatsApp { get; set; }
}