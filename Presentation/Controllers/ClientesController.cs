using Chamapro.Api.Common;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chamapro.Api.Controllers;

[Route(ApiRotas.Prefixo + "/clientes")]
public class ClientesController : ApiControllerBase
{
    private readonly IClienteService _servico;

    public ClientesController(IClienteService servico) => _servico = servico;

    [HttpPut("me")]
    [Authorize(Roles = Perfis.Cliente)]
    public async Task<IActionResult> AtualizarPerfil([FromBody] AtualizarClienteRequest request, CancellationToken ct)
    {
        await _servico.AtualizarPerfilAsync(request, ct);
        return NoContent();
    }
}