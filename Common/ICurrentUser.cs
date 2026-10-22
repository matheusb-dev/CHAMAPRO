using System.Security.Claims;

namespace Chamapro.Api.Common;

/// <summary>
/// Usuário da requisição atual. Os Services recebem esta interface por injeção e
/// nunca leem o HttpContext. No Development ela é preenchida pelo login de desenvolvimento
/// (X-Dev-User-Id / X-Dev-Role); em produção, pelo JWT. O código dos Services é o mesmo nos dois casos.
/// </summary>
public interface ICurrentUser
{
    bool EstaAutenticado { get; }

    /// <summary>Id do usuário. Lança BusinessException 401-equivalente se não houver usuário logado.</summary>
    int Id { get; }

    /// <summary>CLIENTE, PROFISSIONAL ou ADMIN (null se anônimo).</summary>
    string? Perfil { get; }

    bool EhCliente { get; }
    bool EhProfissional { get; }
    bool EhAdmin { get; }
}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool EstaAutenticado => Principal?.Identity?.IsAuthenticated == true;

    public int Id
    {
        get
        {
            // O JWT mapeia "sub" para NameIdentifier por padrão; aceitamos os dois nomes.
            var valor = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? Principal?.FindFirstValue("sub");

            if (!int.TryParse(valor, out var id))
                throw new BusinessException(StatusCodes.Status401Unauthorized, "NAO_AUTENTICADO",
                    "Usuário não autenticado.", "Não autenticado");

            return id;
        }
    }

    public string? Perfil => Principal?.FindFirstValue(ClaimTypes.Role) ?? Principal?.FindFirstValue("role");

    public bool EhCliente => Perfil == Perfis.Cliente;
    public bool EhProfissional => Perfil == Perfis.Profissional;
    public bool EhAdmin => Perfil == Perfis.Admin;
}
