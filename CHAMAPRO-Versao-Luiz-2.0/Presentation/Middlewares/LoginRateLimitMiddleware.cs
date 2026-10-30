using System.Collections.Concurrent;

namespace Chamapro.Api.Middlewares;

public class LoginRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, Queue<DateTime>> Tentativas = new();
    private const int LimitePorMinuto = 5;
    private static readonly TimeSpan Janela = TimeSpan.FromMinutes(1);

    public LoginRateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/v1/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var chave = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var fila = Tentativas.GetOrAdd(chave, _ => new Queue<DateTime>());

        lock (fila)
        {
            var agora = DateTime.UtcNow;
            while (fila.Count > 0 && agora - fila.Peek() > Janela)
                fila.Dequeue();

            if (fila.Count >= LimitePorMinuto)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "application/problem+json";
                context.Response.WriteAsJsonAsync(new
                {
                    status = StatusCodes.Status429TooManyRequests,
                    codigo = "MUITAS_TENTATIVAS",
                    titulo = "Muitas tentativas",
                    detalhe = "Muitas tentativas de login. Tente novamente em alguns minutos."
                }).GetAwaiter().GetResult();
                return;
            }

            fila.Enqueue(agora);
        }

        await _next(context);
    }
}
