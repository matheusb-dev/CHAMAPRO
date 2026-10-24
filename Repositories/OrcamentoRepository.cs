using Chamapro.Api.Data;
using Chamapro.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Repositories;

public class OrcamentoRepository : IOrcamentoRepository
{
    private readonly AppDbContext _db;

    public OrcamentoRepository(AppDbContext db) => _db = db;

    public Task<int?> ObterPedidoIdAsync(int orcamentoId)
        => _db.Set<Orcamento>().AsNoTracking()
            .Where(o => o.Id == orcamentoId)
            .Select(o => (int?)o.PedidoId)
            .FirstOrDefaultAsync();

    public async Task AdicionarAsync(Orcamento orcamento) => await _db.Set<Orcamento>().AddAsync(orcamento);

    public async Task<IReadOnlyList<Orcamento>> ListarDoPedidoAsync(int pedidoId)
        => await _db.Set<Orcamento>().AsNoTracking()
            .Include(o => o.Profissional).ThenInclude(p => p.Usuario)
            .Where(o => o.PedidoId == pedidoId)
            .OrderBy(o => o.Valor).ThenBy(o => o.Id)
            .ToListAsync();

    public async Task<(decimal? Min, decimal? Max)> ObterFaixaPrecoAsync(int profissionalId)
    {
        var faixa = await _db.Set<Profissional>().AsNoTracking()
            .Where(p => p.Id == profissionalId)
            .Select(p => new { p.PrecoMin, p.PrecoMax })
            .FirstOrDefaultAsync();

        return (ParaDecimal(faixa?.PrecoMin), ParaDecimal(faixa?.PrecoMax));
    }

    // Aceita a propriedade tanto como decimal? quanto como object (caso a entity da P1 tenha saído com esse tipo).
    private static decimal? ParaDecimal(object? valor)
        => valor is null ? null : Convert.ToDecimal(valor);
}