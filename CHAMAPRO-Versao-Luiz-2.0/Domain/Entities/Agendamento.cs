using Chamapro.Api.Models.Enums;

namespace Chamapro.Api.Models.Entities;

// Sem propriedade de navegação para Pedido: a relação 1:1 é configurada pelo lado do Pedido (PedidoConfiguration).
public class Agendamento
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public DateTime DataHora { get; set; }
    public StatusAgendamento Status { get; set; } = StatusAgendamento.Agendado;
}
