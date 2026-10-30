using Chamapro.Api.Repositories;

namespace Chamapro.Api.BackgroundServices;

public class CancelamentoPedidosBackgroundService : BackgroundService
{
    private const int DiasParaCancelar = 7;

    private readonly IServiceScopeFactory _escopos;
    private readonly IConfiguration _config;
    private readonly ILogger<CancelamentoPedidosBackgroundService> _logger;

    public CancelamentoPedidosBackgroundService(
        IServiceScopeFactory escopos, IConfiguration config, ILogger<CancelamentoPedidosBackgroundService> logger)
    {
        _escopos = escopos;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var horas = int.TryParse(_config["Rotinas:CancelamentoIntervaloHoras"], out var h) && h > 0 ? h : 24;
        using var timer = new PeriodicTimer(TimeSpan.FromHours(horas));

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

            var limite = DateTime.UtcNow.AddDays(-DiasParaCancelar);
            var cancelados = await repositorio.CancelarPedidosPendentesAsync(limite, ct);

            if (cancelados > 0)
                _logger.LogInformation("Cancelamento automático: {Total} pedido(s) PENDENTE cancelado(s) após {Dias} dias.",
                    cancelados, DiasParaCancelar);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Erro na rotina de cancelamento automático.");
        }
    }
}