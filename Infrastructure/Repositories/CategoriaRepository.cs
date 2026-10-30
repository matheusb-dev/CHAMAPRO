using Chamapro.Api.Models.Entities;

namespace Chamapro.Api.Repositories;

public class CategoriaRepository : ICategoriaRepository
{
    private static readonly object Trava = new();
    private static readonly List<Categoria> Dados = new()
    {
        new Categoria { Id = 1, Nome = "Eletricista", Icone = "zap", Ativa = true },
        new Categoria { Id = 2, Nome = "Encanador", Icone = "droplet", Ativa = true }
    };
    private static int _proximoId = 3;

    public Task<List<Categoria>> ListarAtivasAsync(CancellationToken ct = default)
    {
        lock (Trava)
        {
            var lista = Dados.Where(c => c.Ativa).OrderBy(c => c.Nome).ToList();
            return Task.FromResult(lista);
        }
    }

    public Task<Categoria?> ObterPorIdAsync(int id, CancellationToken ct = default)
    {
        lock (Trava)
        {
            return Task.FromResult(Dados.FirstOrDefault(c => c.Id == id));
        }
    }

    public Task<bool> ExisteNomeAsync(string nome, int? ignorarId = null, CancellationToken ct = default)
    {
        lock (Trava)
        {
            var existe = Dados.Any(c =>
                c.Id != ignorarId &&
                string.Equals(c.Nome, nome, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(existe);
        }
    }

    public Task<bool> TemVinculosAsync(int id, CancellationToken ct = default)
    {
        return Task.FromResult(false);
    }

    public Task<Categoria> AdicionarAsync(Categoria categoria, CancellationToken ct = default)
    {
        lock (Trava)
        {
            categoria.Id = _proximoId++;
            Dados.Add(categoria);
            return Task.FromResult(categoria);
        }
    }

    public Task AtualizarAsync(Categoria categoria, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task RemoverAsync(Categoria categoria, CancellationToken ct = default)
    {
        lock (Trava)
        {
            Dados.Remove(categoria);
            return Task.CompletedTask;
        }
    }
}
