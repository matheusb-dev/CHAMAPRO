using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chamapro.Api.Controllers;

/// <summary>
/// Base de todos os Controllers. Já aplica [ApiController] (validação automática dos DTOs → 400)
/// e [Authorize] (rotas públicas devem declarar [AllowAnonymous]).
/// Rotas: use [Route(ApiRotas.Prefixo + "/categorias")] em cada Controller.
/// </summary>
[ApiController]
[Authorize]
public abstract class ApiControllerBase : ControllerBase
{
}

public static class ApiRotas
{
    public const string Prefixo = "api/v1";
}
