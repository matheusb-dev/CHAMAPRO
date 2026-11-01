namespace Chamapro.Api.Models.Entities;

public class Denuncia
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int DenuncianteId { get; set; }
    public int DenunciadoId { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string Status { get; set; } = "Pendente";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Pedido Pedido { get; set; } = null!;
    public Usuario Denunciante { get; set; } = null!;
    public Usuario Denunciado { get; set; } = null!;
}