using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public class VerificacaoAvaliacaoRequest
{
    [Required]
    public bool Aprovado { get; set; }

    public string? Observacao { get; set; }
}