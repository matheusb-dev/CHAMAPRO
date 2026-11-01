using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Chamapro.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected int UsuarioLogadoId
    {
        get
        {
            if (User?.Identity?.IsAuthenticated != true)
                return 0;

            var claimId = User.FindFirst("id")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(claimId, out var id))
                return id;

            return 0;
        }
    }
}