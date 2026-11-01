using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public sealed class SolicitarRecuperacaoSenhaRequest
{
    [Required, EmailAddress, StringLength(120)]
    public string Email { get; set; } = string.Empty;
}

public sealed class RedefinirSenhaRequest
{
    [Required, StringLength(128, MinimumLength = 40)]
    public string Token { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8)]
    public string NovaSenha { get; set; } = string.Empty;

    [Required, Compare(nameof(NovaSenha))]
    public string ConfirmacaoSenha { get; set; } = string.Empty;
}
