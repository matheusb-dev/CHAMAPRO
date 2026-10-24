using Chamapro.Api.Data;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Chamapro.Api.Repositories;

public class PedidoRepository : IPedidoRepository
{
    private readonly AppDbContext _db;

    public PedidoRepository(AppDbContext db) => _db = db;

    public Task<IDbContextTransaction> IniciarTransacaoAsync() => _db.Database.BeginTransactionAsync();

    public async Task BloquearAsync(int pedidoId)
        => await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM pedidos WHERE id = {pedidoId} FOR UPDATE");

    public Task SalvarAsync() => _db.SaveChangesAsync();

    public Task<int?> ObterProfissionalIdPorUsuarioAsync(int usuarioId)
        => _db.Set<Profissional>().AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync();

    public Task<bool> CategoriaExisteAsync(int categoriaId)
        => _db.Set<Categoria>().AsNoTracking().AnyAsync(c => c.Id == categoriaId);

    public Task<int> ContarPendentesDoClienteAsync(int clienteId)
        => _db.Set<Pedido>().AsNoTracking()
            .CountAsync(p => p.ClienteId == clienteId && p.Status == StatusPedido.Pendente);

    public Task<bool> ClienteEhDonoAsync(int pedidoId, int clienteId)
        => _db.Set<Pedido>().AsNoTracking().AnyAsync(p => p.Id == pedidoId && p.ClienteId == clienteId);

    public async Task<HashSet<int>> FiltrarProfissionaisQueAtendemAsync(
        IReadOnlyList<int> profissionalIds, int categoriaId, string cidade)
    {
        var ids = profissionalIds.ToList();
        var cidadeNormalizada = cidade.Trim().ToLower();

        var atendem = await _db.Set<Profissional>().AsNoTracking()
            .Where(p => ids.Contains(p.Id)
                && _db.Set<ProfissionalCategoria>().Any(pc => pc.ProfissionalId == p.Id && pc.CategoriaId == categoriaId)
                && _db.Set<AreaAtendimento>().Any(a => a.ProfissionalId == p.Id && a.Cidade.ToLower() == cidadeNormalizada))
            .Select(p => p.Id)
            .ToListAsync();

        return atendem.ToHashSet();
    }

    public async Task AdicionarAsync(Pedido pedido) => await _db.Set<Pedido>().AddAsync(pedido);

    public Task<Pedido?> ObterParaAtualizacaoAsync(int id)
        => _db.Set<Pedido>()
            .Include(p => p.Destinatarios)
            .Include(p => p.Orcamentos)
            .Include(p => p.Agendamento)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id);

    public Task<Pedido?> ObterDetalheAsync(int id)
        => _db.Set<Pedido>().AsNoTracking()
            .Include(p => p.Cliente)
            .Include(p => p.Categoria)
            .Include(p => p.Destinatarios).ThenInclude(d => d.Profissional).ThenInclude(pr => pr.Usuario)
            .Include(p => p.Orcamentos)
            .Include(p => p.Agendamento)
            .Include(p => p.Avaliacao)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<(IReadOnlyList<Pedido> Itens, int Total)> ListarAsync(
        int? clienteId, int? profissionalId, StatusPedido? status, int pagina, int tamanho)
    {
        IQueryable<Pedido> consulta = _db.Set<Pedido>().AsNoTracking();

        if (clienteId.HasValue)
            consulta = consulta.Where(p => p.ClienteId == clienteId.Value);

        if (profissionalId.HasValue)
            consulta = consulta.Where(p => p.Destinatarios.Any(d => d.ProfissionalId == profissionalId.Value));

        if (status.HasValue)
            consulta = consulta.Where(p => p.Status == status.Value);

        var total = await consulta.CountAsync();

        var itens = await consulta
            .Include(p => p.Categoria)
            .Include(p => p.Destinatarios)
            .OrderByDescending(p => p.CriadoEm).ThenByDescending(p => p.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync();

        return (itens, total);
    }
}
