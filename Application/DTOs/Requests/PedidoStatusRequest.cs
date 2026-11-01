using System.ComponentModel.DataAnnotations;
using Chamapro.Api.Models.Enums;

namespace Chamapro.Api.DTOs.Requests;

public class PedidoStatusRequest
{
    /// <summary>EM_ANDAMENTO ou CONCLUIDO. Um valor desconhecido faz o binding devolver 400.</summary>
    [Required(ErrorMessage = "O status é obrigatório.")]
    public StatusPedido? Status { get; set; }
}
