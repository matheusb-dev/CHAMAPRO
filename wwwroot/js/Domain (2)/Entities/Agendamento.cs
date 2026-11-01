using Chamapro.Api.Models.Enums;

namespace Chamapro.Api.Models.Entities;

// A relação 1:1 com Pedido é configurada nos dois lados no AppDbContext (HasOne(x => x.Pedido).WithOne(x => x.Agendamento)).
public class Agendamento
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int? ProfissionalId { get; set; }
    public DateTime DataHora { get; set; }
    public StatusAgendamento Status { get; set; } = StatusAgendamento.Agendado;
    public DateTime? LembreteEnviadoEm { get; set; }
    public Pedido Pedido { get; set; } = null!;
}
