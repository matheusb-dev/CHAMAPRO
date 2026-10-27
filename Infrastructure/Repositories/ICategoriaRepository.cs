using Chamapro.Api.Models.Entities;

namespace Chamapro.Api.Repositories;

public interface ICategoriaRepository
{
    Task<List<Categoria>> ListarAtivasAsync(CancellationToken ct = default);

    Task<Categoria?> ObterPorIdAsync(int id, CancellationToken ct = default);

    Task<bool> ExisteNomeAsync(string nome, int? ignorarId = null, CancellationToken ct = default);

    Task<bool> TemVinculosAsync(int id, CancellationToken ct = default);

    Task<Categoria> AdicionarAsync(Categoria categoria, CancellationToken ct = default);
    Task AtualizarAsync(Categoria categoria, CancellationToken ct = default);
    Task RemoverAsync(Categoria categoria, CancellationToken ct = default);
}
