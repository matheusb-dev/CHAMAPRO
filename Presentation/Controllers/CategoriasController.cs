using Chamapro.Api.Common;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chamapro.Api.Controllers;

/// <summary>Endpoints 5 a 8: categorias (listagem pública; criar, editar e remover só ADMIN).</summary>
[Route(ApiRotas.Prefixo + "/categorias")]
public class CategoriasController : ApiControllerBase
{
    private readonly ICategoriaService _servico;

    public CategoriasController(ICategoriaService servico) => _servico = servico;

    /// <summary>5 - GET /categorias: categorias ativas, ordenadas por nome.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<CategoriaResponse>>> Listar(CancellationToken ct)
        => Ok(await _servico.ListarAsync(ct));

    /// <summary>6 - POST /categorias (ADMIN): 201 com a categoria criada.</summary>
    [HttpPost]
    [Authorize(Roles = Perfis.Admin)]
    public async Task<ActionResult<CategoriaResponse>> Criar([FromBody] CategoriaRequest request, CancellationToken ct)
    {
        var criada = await _servico.CriarAsync(request, ct);
        return Created($"/{ApiRotas.Prefixo}/categorias/{criada.Id}", criada);
    }

    /// <summary>7 - PUT /categorias/{id} (ADMIN): 200 com a categoria editada.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Perfis.Admin)]
    public async Task<ActionResult<CategoriaResponse>> Editar(int id, [FromBody] CategoriaRequest request, CancellationToken ct)
        => Ok(await _servico.AtualizarAsync(id, request, ct));

    /// <summary>8 - DELETE /categorias/{id} (ADMIN): 204. Com vínculos, apenas inativa.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Perfis.Admin)]
    public async Task<IActionResult> Remover(int id, CancellationToken ct)
    {
        await _servico.RemoverAsync(id, ct);
        return NoContent();
    }
}
