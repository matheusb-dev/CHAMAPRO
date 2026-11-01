using Chamapro.Api.Models.Enums;

namespace Chamapro.Api.Models.Entities;

public class Orcamento
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int ProfissionalId { get; set; }
    public decimal Valor { get; set; }
    public string PrazoEstimado { get; set; } = string.Empty;
    public string? Condicoes { get; set; }
    public DateTime ValidoAte { get; set; }
    public StatusOrcamento Status { get; set; } = StatusOrcamento.Enviado;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Pedido Pedido { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
}
