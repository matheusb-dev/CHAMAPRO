using System.Security.Claims;
using Chamapro.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Middlewares;

public class UserActiveMiddleware
{
    private readonly RequestDelegate _next;

    public UserActiveMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/api/v1/auth", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (!int.TryParse(userIdValue, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                status = StatusCodes.Status401Unauthorized,
                codigo = "NAO_AUTENTICADO",
                titulo = "Não autenticado",
                detalhe = "Usuário não autenticado."
            });
            return;
        }

        var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (usuario is null || !usuario.Ativo)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                status = StatusCodes.Status401Unauthorized,
                codigo = "USUARIO_INATIVO",
                titulo = "Usuário inativo",
                detalhe = "Sua conta está bloqueada ou inativa."
            });
            return;
        }

        await _next(context);
    }
}
