using Chamapro.Api.Models.Enums;

namespace Chamapro.Api.Models.Entities;

/// <summary>Profissional que recebeu o pedido (de 1 a 5 por pedido) e a situação dele.</summary>
public class PedidoDestinatario
{
    public int PedidoId { get; set; }
    public int ProfissionalId { get; set; }
    public SituacaoDestinatario Situacao { get; set; } = SituacaoDestinatario.Pendente;
    public string? MotivoRecusa { get; set; }

    public Pedido Pedido { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
}
