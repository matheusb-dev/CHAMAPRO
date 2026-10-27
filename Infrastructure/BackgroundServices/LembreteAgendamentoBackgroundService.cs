using System.Net;
using Chamapro.Api.Repositories;
using Chamapro.Api.Services;

namespace Chamapro.Api.BackgroundServices;

public class LembreteAgendamentoBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _escopos;
    private readonly IConfiguration _config;
    private readonly ILogger<LembreteAgendamentoBackgroundService> _logger;

    public LembreteAgendamentoBackgroundService(
        IServiceScopeFactory escopos, IConfiguration config, ILogger<LembreteAgendamentoBackgroundService> logger)
    {
        _escopos = escopos;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutos = int.TryParse(_config["Rotinas:LembreteIntervaloMinutos"], out var m) && m > 0 ? m : 15;
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutos));

        try
        {
            do
            {
                await ExecutarUmaVezAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ExecutarUmaVezAsync(CancellationToken ct)
    {
        try
        {
            using var escopo = _escopos.CreateScope();
            var repositorio = escopo.ServiceProvider.GetRequiredService<IRotinasRepository>();
            var email = escopo.ServiceProvider.GetRequiredService<IEmailService>();

            var agora = DateTime.UtcNow;
            var lembretes = await repositorio.ListarAgendamentosParaLembreteAsync(agora, agora.AddHours(24), ct);

            foreach (var lembrete in lembretes)
            {
                try
                {
                    var quando = FormatarData(lembrete.DataHoraUtc);
                    var titulo = WebUtility.HtmlEncode(lembrete.Titulo);

                    await email.EnviarAsync(
                        lembrete.ClienteEmail,
                        "Lembrete: serviço agendado para breve",
                        $"<p>Olá, {WebUtility.HtmlEncode(lembrete.ClienteNome)}!</p>" +
                        $"<p>Lembrete do serviço <strong>{titulo}</strong> com {WebUtility.HtmlEncode(lembrete.ProfissionalNome)}, agendado para {quando}.</p>",
                        ct);

                    await email.EnviarAsync(
                        lembrete.ProfissionalEmail,
                        "Lembrete: serviço agendado para breve",
                        $"<p>Olá, {WebUtility.HtmlEncode(lembrete.ProfissionalNome)}!</p>" +
                        $"<p>Lembrete do serviço <strong>{titulo}</strong> com {WebUtility.HtmlEncode(lembrete.ClienteNome)}, agendado para {quando}.</p>",
                        ct);

                    await repositorio.MarcarLembreteEnviadoAsync(lembrete.AgendamentoId, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Falha ao enviar o lembrete do agendamento {AgendamentoId}", lembrete.AgendamentoId);
                }
            }

            if (lembretes.Count > 0)
                _logger.LogInformation("Rotina de lembretes: {Total} agendamento(s) processado(s).", lembretes.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Erro na rotina de lembretes.");
        }
    }

    private static string FormatarData(DateTime utc)
    {
        try
        {
            var fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            return TimeZoneInfo.ConvertTimeFromUtc(utc, fuso).ToString("dd/MM/yyyy 'às' HH:mm");
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return utc.ToString("dd/MM/yyyy 'às' HH:mm") + " (UTC)";
        }
    }
}