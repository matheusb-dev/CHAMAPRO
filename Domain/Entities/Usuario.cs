using Chamapro.Api.Models.Enums;

namespace Chamapro.Api.Models.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public TipoUsuario Tipo { get; set; }
    public bool IdentidadeVerificada { get; set; }
    public bool Ativo { get; set; } = true;

    public string? Telefone { get; set; }
    public bool WhatsAppAtivo { get; set; } = false;
    public string? MensagemInicialWhatsApp { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Profissional? Profissional { get; set; }
}