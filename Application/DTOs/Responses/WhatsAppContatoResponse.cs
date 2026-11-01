namespace Chamapro.Api.DTOs.Responses;

public class WhatsAppContatoResponse
{
    public int UsuarioId { get; set; }
    public string? Telefone { get; set; }
    public bool WhatsAppAtivo { get; set; }
    public string? MensagemInicialWhatsApp { get; set; }
}