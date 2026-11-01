using Chamapro.Api.Common;
using Chamapro.Api.Data;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Chamapro.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Controllers;

[Route(ApiRotas.Prefixo + "/pedidos")]
[Authorize]
public class OrcamentosController : ApiControllerBase
{
    private readonly AppDbContext _context;

    public OrcamentosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{pedidoId:int}/orcamentos")]
    public async Task<ActionResult<IEnumerable<OrcamentoComProfissionalResponse>>> ListarDoPedidoAsync(
        int pedidoId,
        CancellationToken ct)
    {
        var orcamentos = await _context.Orcamentos
            .AsNoTracking()
            .Include(o => o.Profissional)
                .ThenInclude(p => p.Usuario)
            .Where(o => o.PedidoId == pedidoId)
            .OrderBy(o => o.Valor)
            .ToListAsync(ct);

        var itens = orcamentos.Select(o => new OrcamentoComProfissionalResponse(
            o.Id,
            o.PedidoId,
            o.Valor,
            o.PrazoEstimado,
            o.Condicoes,
            o.ValidoAte,
            o.ValidoAte <= DateTime.UtcNow,
            o.Status,
            new ProfissionalOrcamentoResponse(
                o.Profissional.Id,
                o.Profissional.UsuarioId,
                o.Profissional.Usuario.Nome,
                o.Profissional.MediaAvaliacao,
                o.Profissional.Usuario.IdentidadeVerificada))).ToList();

        return Ok(itens);
    }
}