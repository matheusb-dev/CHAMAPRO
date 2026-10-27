namespace Chamapro.Api.Repositories;

public record LembreteAgendamento(
    int AgendamentoId,
    string Titulo,
    DateTime DataHoraUtc,
    string ClienteNome,
    string ClienteEmail,
    string ProfissionalNome,
    string ProfissionalEmail);

public interface IRotinasRepository
{
    Task<List<LembreteAgendamento>> ListarAgendamentosParaLembreteAsync(
        DateTime agoraUtc, DateTime limiteUtc, CancellationToken ct = default);

    Task MarcarLembreteEnviadoAsync(int agendamentoId, CancellationToken ct = default);

    Task<int> CancelarPedidosPendentesAsync(DateTime criadosAntesDeUtc, CancellationToken ct = default);
}