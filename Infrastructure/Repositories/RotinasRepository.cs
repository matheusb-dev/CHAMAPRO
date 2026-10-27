namespace Chamapro.Api.Repositories;

public class RotinasRepository : IRotinasRepository
{
    private static readonly object Trava = new();

    private static readonly List<LembreteAgendamento> Agendamentos = new()
    {
        new LembreteAgendamento(
            1, "Troca de disjuntor", DateTime.UtcNow.AddHours(2),
            "Maria Souza", "maria@exemplo.local",
            "Carlos Silva", "carlos@exemplo.local")
    };

    private static readonly HashSet<int> LembretesEnviados = new();

    public Task<List<LembreteAgendamento>> ListarAgendamentosParaLembreteAsync(
        DateTime agoraUtc, DateTime limiteUtc, CancellationToken ct = default)
    {
        lock (Trava)
        {
            var lista = Agendamentos
                .Where(a => !LembretesEnviados.Contains(a.AgendamentoId)
                         && a.DataHoraUtc >= agoraUtc
                         && a.DataHoraUtc <= limiteUtc)
                .ToList();
            return Task.FromResult(lista);
        }
    }

    public Task MarcarLembreteEnviadoAsync(int agendamentoId, CancellationToken ct = default)
    {
        lock (Trava)
        {
            LembretesEnviados.Add(agendamentoId);
        }
        return Task.CompletedTask;
    }

    public Task<int> CancelarPedidosPendentesAsync(DateTime criadosAntesDeUtc, CancellationToken ct = default)
    {
        return Task.FromResult(0);
    }
}