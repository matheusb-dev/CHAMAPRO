namespace Chamapro.Api.DTOs.Requests;

public class AgendamentoCriarRequest
{
    public int PedidoId { get; set; }
    public DateTime DataHora { get; set; }
}