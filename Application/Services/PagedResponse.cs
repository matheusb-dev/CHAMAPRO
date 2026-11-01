namespace Chamapro.Api.Services;

/// <summary>Resposta paginada de GET /pedidos: { itens, pagina, tamanho, total } (mesmo formato de RespostaPaginada).</summary>
public class PagedResponse<T>
{
    public IReadOnlyList<T> Itens { get; }
    public int Pagina { get; }
    public int Tamanho { get; }
    public int Total { get; }

    public PagedResponse(IReadOnlyList<T> itens, int pagina, int tamanho, int total)
    {
        Itens = itens;
        Pagina = pagina;
        Tamanho = tamanho;
        Total = total;
    }
}
