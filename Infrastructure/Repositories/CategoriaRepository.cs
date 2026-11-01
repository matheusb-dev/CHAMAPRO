using Chamapro.Api.Data;
using Chamapro.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Repositories;

public class CategoriaRepository : ICategoriaRepository
{
    private readonly AppDbContext _db;

    public CategoriaRepository(AppDbContext db) => _db = db;

    public Task<List<Categoria>> ListarAtivasAsync(CancellationToken ct = default)
        => _db.Categorias.AsNoTracking()
            .Where(c => c.Ativa)
            .OrderBy(c => c.Nome)
            .ToListAsync(ct);

    public Task<Categoria?> ObterPorIdAsync(int id, CancellationToken ct = default)
        => _db.Categorias.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExisteNomeAsync(string nome, int? ignorarId = null, CancellationToken ct = default)
    {
        var normalizado = nome.Trim().ToLowerInvariant();
        return _db.Categorias.AsNoTracking().AnyAsync(c =>
            c.Id != ignorarId && c.Nome.ToLower() == normalizado, ct);
    }

    public async Task<bool> TemVinculosAsync(int id, CancellationToken ct = default)
        => await _db.ProfissionalCategorias.AsNoTracking().AnyAsync(x => x.CategoriaId == id, ct)
            || await _db.Pedidos.AsNoTracking().AnyAsync(p => p.CategoriaId == id, ct);

    public async Task<Categoria> AdicionarAsync(Categoria categoria, CancellationToken ct = default)
    {
        await _db.Categorias.AddAsync(categoria, ct);
        await _db.SaveChangesAsync(ct);
        return categoria;
    }

    public async Task AtualizarAsync(Categoria categoria, CancellationToken ct = default)
    {
        _db.Categorias.Update(categoria);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoverAsync(Categoria categoria, CancellationToken ct = default)
    {
        _db.Categorias.Remove(categoria);
        await _db.SaveChangesAsync(ct);
    }
}
