namespace Chamapro.Api.Models.Entities;

// Sem propriedade de navegação para Pedido: a relação 1:1 é configurada pelo lado do Pedido (PedidoConfiguration).
public class Avaliacao
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int AvaliadorId { get; set; }
    public int AvaliadoId { get; set; }
    public int Nota { get; set; }
    public string? Comentario { get; set; }
}
