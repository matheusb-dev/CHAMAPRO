using Chamapro.Api.Common;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Data;
using Chamapro.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Controllers;

[Route(ApiRotas.Prefixo + "/categorias")]
public class CategoriasController : ApiControllerBase
{
    private readonly ICategoriaService _servico;
    private readonly AppDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditoriaAdminService _auditoria;

    public CategoriasController(ICategoriaService servico, AppDbContext context, ICurrentUser currentUser,
        IAuditoriaAdminService auditoria)
    {
        _servico = servico;
        _context = context;
        _currentUser = currentUser;
        _auditoria = auditoria;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<CategoriaResponse>>> Listar(CancellationToken ct)
        => Ok(await _servico.ListarAsync(ct));

    [HttpPost]
    [Authorize(Roles = Perfis.Admin)]
    public async Task<ActionResult<CategoriaResponse>> Criar([FromBody] CategoriaRequest request, CancellationToken ct)
    {
        var criada = await _servico.CriarAsync(request, ct);
        _auditoria.Registrar(_currentUser.Id, "CATEGORIA_CRIADA", "CATEGORIA", criada.Id,
            estadoNovo: criada.Nome);
        await _context.SaveChangesAsync(ct);
        return Created($"/{ApiRotas.Prefixo}/categorias/{criada.Id}", criada);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Perfis.Admin)]
    public async Task<ActionResult<CategoriaResponse>> Editar(int id, [FromBody] CategoriaRequest request, CancellationToken ct)
    {
        var anterior = await _context.Categorias.AsNoTracking()
            .Where(c => c.Id == id).Select(c => c.Nome).FirstOrDefaultAsync(ct)
            ?? throw BusinessException.NaoEncontrado("CATEGORIA_NAO_ENCONTRADA", "Categoria não encontrada.");
        var atualizada = await _servico.AtualizarAsync(id, request, ct);
        _auditoria.Registrar(_currentUser.Id, "CATEGORIA_EDITADA", "CATEGORIA", atualizada.Id,
            anterior, atualizada.Nome);
        await _context.SaveChangesAsync(ct);
        return Ok(atualizada);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Perfis.Admin)]
    public async Task<IActionResult> Remover(int id, CancellationToken ct)
    {
        var nome = await _context.Categorias.AsNoTracking()
            .Where(c => c.Id == id).Select(c => c.Nome).FirstOrDefaultAsync(ct)
            ?? throw BusinessException.NaoEncontrado("CATEGORIA_NAO_ENCONTRADA", "Categoria não encontrada.");
        await _servico.RemoverAsync(id, ct);
        var foiDesativada = await _context.Categorias.AsNoTracking().AnyAsync(c => c.Id == id, ct);
        _auditoria.Registrar(_currentUser.Id, foiDesativada ? "CATEGORIA_DESATIVADA" : "CATEGORIA_REMOVIDA",
            "CATEGORIA", id, nome, foiDesativada ? "INATIVA" : "REMOVIDA");
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }
}
