using System.Collections.Concurrent;

namespace Chamapro.Api.Middlewares;

public class LoginRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, JanelaTentativas> Tentativas = new();
    private static readonly TimeSpan Janela = TimeSpan.FromMinutes(1);
    private static int _requisicoes;

    private sealed class JanelaTentativas
    {
        private readonly Queue<DateTime> _instantes = new();
        private readonly object _sync = new();
        private DateTime _ultimaRequisicaoUtc;

        public bool TentarRegistrar(DateTime agoraUtc, int limite)
        {
            lock (_sync)
            {
                _ultimaRequisicaoUtc = agoraUtc;
                while (_instantes.Count > 0 && agoraUtc - _instantes.Peek() >= Janela)
                    _instantes.Dequeue();

                if (_instantes.Count >= limite)
                    return false;

                _instantes.Enqueue(agoraUtc);
                return true;
            }
        }

        public bool EstaExpirada(DateTime agoraUtc)
        {
            lock (_sync)
            {
                while (_instantes.Count > 0 && agoraUtc - _instantes.Peek() >= Janela)
                    _instantes.Dequeue();
                return _instantes.Count == 0 && agoraUtc - _ultimaRequisicaoUtc >= Janela;
            }
        }
    }

    public LoginRateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        var limite = path.StartsWithSegments("/api/v1/auth/login", StringComparison.OrdinalIgnoreCase) ? 5
            : path.StartsWithSegments("/api/v1/auth/cadastro/cliente", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/api/v1/auth/cadastro/profissional", StringComparison.OrdinalIgnoreCase) ? 3
            : path.StartsWithSegments("/api/v1/auth/recuperar-senha", StringComparison.OrdinalIgnoreCase) ? 3
            : path.StartsWithSegments("/api/v1/auth/redefinir-senha", StringComparison.OrdinalIgnoreCase) ? 10
            : 0;
        if (limite == 0)
        {
            await _next(context);
            return;
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var chave = $"{path.Value?.ToLowerInvariant()}|{ip}";
        var bucket = Tentativas.GetOrAdd(chave, _ => new JanelaTentativas());
        var agora = DateTime.UtcNow;
        var permitido = bucket.TentarRegistrar(agora, limite);

        if (Interlocked.Increment(ref _requisicoes) % 256 == 0)
        {
            foreach (var (chaveExpirada, janela) in Tentativas)
            {
                if (janela.EstaExpirada(agora))
                    ((ICollection<KeyValuePair<string, JanelaTentativas>>)Tentativas)
                        .Remove(new KeyValuePair<string, JanelaTentativas>(chaveExpirada, janela));
            }
        }

        if (!permitido)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/problem+json";
            context.Response.Headers.RetryAfter = "60";
            await context.Response.WriteAsJsonAsync(new
            {
                status = StatusCodes.Status429TooManyRequests,
                codigo = "MUITAS_TENTATIVAS",
                titulo = "Muitas tentativas",
                detalhe = "Muitas tentativas. Aguarde antes de tentar novamente."
            }, context.RequestAborted);
            return;
        }

        await _next(context);
    }
}
