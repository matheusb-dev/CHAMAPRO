namespace Chamapro.Api.Models.Entities;

public sealed class HistoricoPedido
{
    public long Id { get; set; }
    public int PedidoId { get; set; }
    public int? AtorId { get; set; }
    public string? StatusAnterior { get; set; }
    public string StatusNovo { get; set; } = string.Empty;
    public string? Detalhe { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
