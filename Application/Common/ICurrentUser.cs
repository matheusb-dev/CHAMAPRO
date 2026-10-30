using System.Security.Claims;

namespace Chamapro.Api.Common;

public interface ICurrentUser
{
    bool EstaAutenticado { get; }
    int Id { get; }
    string? Perfil { get; }
    bool EhCliente { get; }
    bool EhProfissional { get; }
    bool EhAdmin { get; }
    string? Role { get; }
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
            var valor = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? Principal?.FindFirstValue("sub");

            if (!int.TryParse(valor, out var id))
                throw new BusinessException(StatusCodes.Status401Unauthorized, "NAO_AUTENTICADO", "Usuário não autenticado.", "Não autenticado");

            return id;
        }
    }

    public string? Perfil => Principal?.FindFirstValue(ClaimTypes.Role) ?? Principal?.FindFirstValue("role");

    public bool EhCliente => Perfil == Perfis.Cliente;
    public bool EhProfissional => Perfil == Perfis.Profissional;
    public bool EhAdmin => Perfil == Perfis.Admin;

    public string? Role => Perfil;
}
