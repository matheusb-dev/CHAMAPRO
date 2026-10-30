using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Common;

/// <summary>Parâmetros de paginação da query string: ?pagina=1&amp;tamanho=10 (tamanho máximo 50).</summary>
public class PaginacaoQuery
{
    public const int TamanhoPadrao = 10;
    public const int TamanhoMaximo = 50;

    private int _pagina = 1;
    private int _tamanho = TamanhoPadrao;

    public int Pagina
    {
        get => _pagina;
        set => _pagina = value < 1 ? 1 : value;
    }

    public int Tamanho
    {
        get => _tamanho;
        set => _tamanho = value < 1 ? TamanhoPadrao : Math.Min(value, TamanhoMaximo);
    }
}

/// <summary>Resposta paginada padrão: { itens, pagina, tamanho, total }.</summary>
public class RespostaPaginada<T>
{
    public IReadOnlyList<T> Itens { get; init; } = Array.Empty<T>();
    public int Pagina { get; init; }
    public int Tamanho { get; init; }
    public int Total { get; init; }
}

public static class PaginacaoExtensions
{
    /// <summary>
    /// Aplica a paginação em uma consulta EF Core. Use depois de filtrar e ordenar.
    /// Para converter Entity em DTO, faça um .Select(...) antes de chamar.
    /// </summary>
    public static async Task<RespostaPaginada<T>> ParaPaginadoAsync<T>(
        this IQueryable<T> consulta, PaginacaoQuery paginacao, CancellationToken ct = default)
    {
        var total = await consulta.CountAsync(ct);
        var itens = await consulta
            .Skip((paginacao.Pagina - 1) * paginacao.Tamanho)
            .Take(paginacao.Tamanho)
            .ToListAsync(ct);

        return new RespostaPaginada<T>
        {
            Itens = itens,
            Pagina = paginacao.Pagina,
            Tamanho = paginacao.Tamanho,
            Total = total
        };
    }
}
