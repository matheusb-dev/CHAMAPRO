using Chamapro.Api.Data;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Chamapro.Api.Models.Time;
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
            .Where(p => p.UsuarioId == usuarioId && p.Usuario.Ativo)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync();

    // The creation history marks automatic distribution without adding a schema column.
    // Manual orders never enter this open queue, including when their recipients are blocked.
    private IQueryable<Pedido> OportunidadesAbertas(int profissionalId)
        => ConsultarAbertas(_db, profissionalId);

    internal static IQueryable<Pedido> ConsultarAbertas(AppDbContext db, int profissionalId)
        => db.Pedidos.Where(p => p.Status == StatusPedido.Pendente
            && db.HistoricosPedidos.Any(h => h.PedidoId == p.Id
                && h.Detalhe == "Chamado urgente aberto a profissionais compatíveis.")
            && db.Profissionais.Any(pr => pr.Id == profissionalId
                && pr.Usuario.Ativo && pr.Usuario.IdentidadeVerificada
                && pr.Categorias.Any(c => c.CategoriaId == p.CategoriaId && c.Categoria.Ativa)
                && pr.Areas.Any(a => a.Cidade.ToLower() == p.Cidade.ToLower()
                    && (p.Bairro == null || a.Bairro == null || a.Bairro.ToLower() == p.Bairro.ToLower()))));

    public Task<bool> DistribuicaoAbertaAsync(int pedidoId)
        => _db.HistoricosPedidos.AnyAsync(h => h.PedidoId == pedidoId
            && h.Detalhe == "Chamado urgente aberto a profissionais compatíveis.");

    public Task<bool> PodeReceberAbertoAsync(int pedidoId, int profissionalId)
        => OportunidadesAbertas(profissionalId).AnyAsync(p => p.Id == pedidoId);

    public Task<bool> CategoriaExisteAsync(int categoriaId)
        => _db.Set<Categoria>().AsNoTracking().AnyAsync(c => c.Id == categoriaId && c.Ativa);

    public Task<int> ContarPendentesDoClienteAsync(int clienteId)
        => _db.Set<Pedido>().AsNoTracking()
            .CountAsync(p => p.ClienteId == clienteId && p.Status == StatusPedido.Pendente);

    public Task<bool> ClienteEhDonoAsync(int pedidoId, int clienteId)
        => _db.Set<Pedido>().AsNoTracking().AnyAsync(p => p.Id == pedidoId && p.ClienteId == clienteId);

    public void AdicionarHistorico(int pedidoId, int? atorId, StatusPedido? anterior, StatusPedido novo, string detalhe)
        => _db.HistoricosPedidos.Add(new HistoricoPedido
        {
            PedidoId = pedidoId,
            AtorId = atorId,
            StatusAnterior = anterior?.ToString(),
            StatusNovo = novo.ToString(),
            Detalhe = detalhe,
            CriadoEm = DateTime.UtcNow
        });

    public async Task<IReadOnlyList<PedidoHistoricoResponse>?> ListarHistoricoAsync(
        int pedidoId, int? clienteId, int? profissionalId)
    {
        var autorizado = await _db.Pedidos.AsNoTracking().AnyAsync(p => p.Id == pedidoId
            && ((clienteId.HasValue && p.ClienteId == clienteId.Value)
                || (profissionalId.HasValue && p.Destinatarios.Any(d => d.ProfissionalId == profissionalId.Value))));
        if (!autorizado && !(profissionalId.HasValue && await PodeReceberAbertoAsync(pedidoId, profissionalId.Value))) return null;

        return await _db.HistoricosPedidos.AsNoTracking()
            .Where(h => h.PedidoId == pedidoId)
            .OrderBy(h => h.CriadoEm).ThenBy(h => h.Id)
            .Select(h => new PedidoHistoricoResponse(h.Id, h.StatusAnterior, h.StatusNovo, h.Detalhe, h.CriadoEm))
            .ToListAsync();
    }

    public async Task<HashSet<int>> FiltrarProfissionaisQueAtendemAsync(
        IReadOnlyList<int> profissionalIds, int categoriaId, string cidade, string? bairro,
        TipoAtendimento tipoAtendimento, DateTime? dataDesejada)
    {
        var ids = profissionalIds.ToList();
        var cidadeNormalizada = cidade.Trim().ToLower();
        var bairroNormalizado = string.IsNullOrWhiteSpace(bairro) ? null : bairro.Trim().ToLower();

        var atendem = await _db.Set<Profissional>().AsNoTracking()
            .Where(p => ids.Contains(p.Id)
                && p.Usuario.Ativo
                && p.Usuario.IdentidadeVerificada
                && _db.Set<ProfissionalCategoria>().Any(pc => pc.ProfissionalId == p.Id
                    && pc.CategoriaId == categoriaId && pc.Categoria.Ativa)
                && _db.Set<AreaAtendimento>().Any(a => a.ProfissionalId == p.Id
                    && a.Cidade.ToLower() == cidadeNormalizada
                    && (bairroNormalizado == null || a.Bairro == null || a.Bairro.ToLower() == bairroNormalizado)))
            .Select(p => p.Id)
            .ToListAsync();

        return atendem.ToHashSet();
    }

    public Task<List<int>> SelecionarProfissionaisCompativeisAsync(
        int categoriaId, string cidade, string? bairro, TipoAtendimento tipoAtendimento,
        DateTime? dataDesejada, int limite = 5)
    {
        var cidadeNormalizada = cidade.Trim().ToLower();
        var bairroNormalizado = string.IsNullOrWhiteSpace(bairro) ? null : bairro.Trim().ToLower();

        return _db.Set<Profissional>().AsNoTracking()
            .Where(p => p.Usuario.Ativo && p.Usuario.IdentidadeVerificada
                && p.Categorias.Any(pc => pc.CategoriaId == categoriaId && pc.Categoria.Ativa)
                && p.Areas.Any(a => a.Cidade.ToLower() == cidadeNormalizada
                    && (bairroNormalizado == null || a.Bairro == null || a.Bairro.ToLower() == bairroNormalizado)))
            .OrderByDescending(p => p.MediaAvaliacao)
            .ThenBy(p => p.Id)
            .Select(p => p.Id)
            .Take(Math.Clamp(limite, 1, 5))
            .ToListAsync();
    }

    public Task<List<int>> ListarUsuarioIdsProfissionaisAsync(IReadOnlyList<int> profissionalIds)
        => _db.Set<Profissional>().AsNoTracking()
            .Where(p => profissionalIds.Contains(p.Id))
            .Select(p => p.UsuarioId)
            .ToListAsync();

    public async Task AdicionarAsync(Pedido pedido) => await _db.Set<Pedido>().AddAsync(pedido);

    public Task<Pedido?> ObterParaAtualizacaoAsync(int id)
        => _db.Set<Pedido>()
            .Include(p => p.Destinatarios)
            .Include(p => p.Orcamentos).ThenInclude(o => o.Profissional).ThenInclude(pr => pr.Usuario)
            .Include(p => p.Agendamento)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id);

    public Task<Pedido?> ObterDetalheAsync(int id)
        => _db.Set<Pedido>().AsNoTracking()
            .Include(p => p.Cliente)
            .Include(p => p.Categoria)
            .Include(p => p.Destinatarios).ThenInclude(d => d.Profissional).ThenInclude(pr => pr.Usuario)
            .Include(p => p.Orcamentos).ThenInclude(o => o.Profissional).ThenInclude(pr => pr.Usuario)
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
        {
            var abertas = OportunidadesAbertas(profissionalId.Value).Select(p => p.Id);
            consulta = consulta.Where(p => p.Destinatarios.Any(d => d.ProfissionalId == profissionalId.Value)
                || abertas.Contains(p.Id));
        }

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
