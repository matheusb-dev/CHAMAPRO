using System.Security.Claims;
using System.Text.Encodings.Web;
using Chamapro.Api.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Chamapro.Api.Middlewares;

public static class DevAuthenticationDefaults
{
    public const string Scheme = "DevAuth";
    public const string HeaderUserId = "X-Dev-User-Id";
    public const string HeaderRole = "X-Dev-Role";
}

public class DevAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public DevAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(DevAuthenticationDefaults.HeaderUserId, out var idHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!int.TryParse(idHeader.ToString(), out var id) || id <= 0)
            return Task.FromResult(AuthenticateResult.Fail($"{DevAuthenticationDefaults.HeaderUserId} inválido."));

        var perfil = Request.Headers[DevAuthenticationDefaults.HeaderRole].ToString().Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(perfil)) perfil = Perfis.Cliente;

        if (!Perfis.Todos.Contains(perfil))
            return Task.FromResult(AuthenticateResult.Fail(
                $"{DevAuthenticationDefaults.HeaderRole} deve ser CLIENTE, PROFISSIONAL ou ADMIN."));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim("sub", id.ToString()),
            new Claim(ClaimTypes.Name, $"dev-user-{id}"),
            new Claim(ClaimTypes.Email, $"dev{id}@chamapro.local"),
            new Claim(ClaimTypes.Role, perfil)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
