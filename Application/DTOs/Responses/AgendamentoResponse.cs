public class AgendamentoResponse
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public DateTime DataHora { get; set; }
    public string Status { get; set; } = string.Empty;
}