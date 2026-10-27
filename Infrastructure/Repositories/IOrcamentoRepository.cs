using Chamapro.Api.Models.Entities;

namespace Chamapro.Api.Repositories;

public interface IOrcamentoRepository
{
    Task<int?> ObterPedidoIdAsync(int orcamentoId);
    Task AdicionarAsync(Orcamento orcamento);
    /// <summary>Orçamentos do pedido com o resumo do profissional, do menor para o maior valor.</summary>
    Task<IReadOnlyList<Orcamento>> ListarDoPedidoAsync(int pedidoId);
    Task<(decimal? Min, decimal? Max)> ObterFaixaPrecoAsync(int profissionalId);
}
