using Chamapro.Api.Common;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chamapro.Api.Controllers;

[Route(ApiRotas.Prefixo + "/categorias")]
public class CategoriasController : ApiControllerBase
{
    private readonly ICategoriaService _servico;

    public CategoriasController(ICategoriaService servico) => _servico = servico;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<CategoriaResponse>>> Listar(CancellationToken ct)
        => Ok(await _servico.ListarAsync(ct));

    [AllowAnonymous]
    [HttpPost]
    [Authorize(Roles = Perfis.Admin)]
    public async Task<ActionResult<CategoriaResponse>> Criar([FromBody] CategoriaRequest request, CancellationToken ct)
    {
        var criada = await _servico.CriarAsync(request, ct);
        return Created($"/{ApiRotas.Prefixo}/categorias/{criada.Id}", criada);
    }

    [AllowAnonymous]
    [HttpPut("{id:int}")]
    [Authorize(Roles = Perfis.Admin)]
    public async Task<ActionResult<CategoriaResponse>> Editar(int id, [FromBody] CategoriaRequest request, CancellationToken ct)
        => Ok(await _servico.AtualizarAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Perfis.Admin)]
    public async Task<IActionResult> Remover(int id, CancellationToken ct)
    {
        await _servico.RemoverAsync(id, ct);
        return NoContent();
    }
}
