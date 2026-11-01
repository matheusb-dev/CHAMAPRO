using Chamapro.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chamapro.Api.Controllers;

/// <summary>
/// Apoio ao desenvolvimento: confere se o login de desenvolvimento está funcionando.
/// Só responde no ambiente Development (404 nos demais). Remover na integração.
/// </summary>
[Route(ApiRotas.Prefixo + "/dev")]
public class DevController : ApiControllerBase
{
    private readonly ICurrentUser _usuario;
    private readonly IWebHostEnvironment _env;

    public DevController(ICurrentUser usuario, IWebHostEnvironment env)
    {
        _usuario = usuario;
        _env = env;
    }

    /// <summary>GET /api/v1/dev/whoami com X-Dev-User-Id e X-Dev-Role devolve o usuário que a API enxerga.</summary>
    [HttpGet("whoami")]
    [AllowAnonymous]
    public IActionResult QuemSouEu()
    {
        if (!_env.IsDevelopment()) return NotFound();

        return Ok(new
        {
            autenticado = _usuario.EstaAutenticado,
            id = _usuario.EstaAutenticado ? _usuario.Id : (int?)null,
            perfil = _usuario.Perfil
        });
    }
}
