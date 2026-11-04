using Chamapro.Api.Data;
using Chamapro.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Repositories;

public class RotinasRepository : IRotinasRepository
{
    private readonly AppDbContext _db;

    public RotinasRepository(AppDbContext db) => _db = db;

    public async Task<List<LembreteAgendamento>> ListarAgendamentosParaLembreteAsync(
        DateTime agoraUtc, DateTime limiteUtc, CancellationToken ct = default)
        => await _db.Agendamentos.AsNoTracking()
            .Where(a => a.LembreteEnviadoEm == null
                && a.DataHora >= agoraUtc && a.DataHora <= limiteUtc
                && (a.Status == StatusAgendamento.Agendado || a.Status == StatusAgendamento.Confirmado)
                && a.Pedido.Status == StatusPedido.Agendado
                && a.Pedido.ProfissionalId != null)
            .Select(a => new LembreteAgendamento(
                a.Id,
                a.Pedido.Titulo,
                a.DataHora,
                a.Pedido.Cliente.Nome,
                a.Pedido.Cliente.Email,
                a.Pedido.Profissional!.Usuario.Nome,
                a.Pedido.Profissional!.Usuario.Email))
            .ToListAsync(ct);

    public async Task MarcarLembreteEnviadoAsync(int agendamentoId, CancellationToken ct = default)
    {
        await _db.Agendamentos
            .Where(a => a.Id == agendamentoId && a.LembreteEnviadoEm == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.LembreteEnviadoEm, DateTime.UtcNow), ct);
    }

    public Task<int> CancelarPedidosPendentesAsync(DateTime criadosAntesDeUtc, CancellationToken ct = default)
        => _db.Pedidos
            .Where(p => p.Status == StatusPedido.Pendente && p.TipoAtendimento != TipoAtendimento.Imediato && p.CriadoEm <= criadosAntesDeUtc)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Status, StatusPedido.Cancelado)
                .SetProperty(p => p.MotivoCancelamento, "Cancelado automaticamente após sete dias sem propostas.")
                .SetProperty(p => p.CancelamentoTardio, false)
                .SetProperty(p => p.CanceladoEm, DateTime.UtcNow), ct);
}
