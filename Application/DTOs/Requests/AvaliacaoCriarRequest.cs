using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public sealed class AvaliacaoCriarRequest
{
    [Range(1, int.MaxValue)]
    public int PedidoId { get; set; }

    [Range(1, 5, ErrorMessage = "A nota deve ficar entre 1 e 5.")]
    public int Nota { get; set; }

    [StringLength(1000)]
    public string? Comentario { get; set; }
}
