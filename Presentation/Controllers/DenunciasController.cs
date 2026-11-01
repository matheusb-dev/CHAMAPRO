using Chamapro.Api.Common;
using Chamapro.Api.Data;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Controllers;

[Route(ApiRotas.Prefixo + "/denuncias")]
public class DenunciasController : ApiControllerBase
{
    private readonly AppDbContext _context;

    public DenunciasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<DenunciaResponse>> CriarAsync(
        [FromBody] DenunciaRequest request,
        CancellationToken ct)
    {
        if (request.PedidoId <= 0)
            throw BusinessException.Invalido("PEDIDO_INVALIDO", "Pedido inválido.");

        if (request.DenunciadoId <= 0)
            throw BusinessException.Invalido("DENUNCIADO_INVALIDO", "Denunciado inválido.");

        var pedidoExiste = await _context.Pedidos
            .AnyAsync(p => p.Id == request.PedidoId, ct);

        if (!pedidoExiste)
            throw BusinessException.NaoEncontrado("PEDIDO_NAO_ENCONTRADO", "Pedido não encontrado.");

        var jaExiste = await _context.Denuncias
            .AnyAsync(d =>
                d.PedidoId == request.PedidoId &&
                d.DenuncianteId == UsuarioLogadoId &&
                d.DenunciadoId == request.DenunciadoId, ct);

        if (jaExiste)
            throw BusinessException.Invalido("DENUNCIA_DUPLICADA", "Você já registrou uma denúncia para este pedido.");

        var denuncia = new Denuncia
        {
            PedidoId = request.PedidoId,
            DenuncianteId = UsuarioLogadoId,
            DenunciadoId = request.DenunciadoId,
            Motivo = request.Motivo.Trim(),
            Tipo = request.Tipo.Trim(),
            Descricao = string.IsNullOrWhiteSpace(request.Descricao) ? null : request.Descricao.Trim(),
            Status = "Pendente",
            CriadoEm = DateTime.UtcNow
        };

        await _context.Denuncias.AddAsync(denuncia, ct);
        await _context.SaveChangesAsync(ct);

        return Ok(new DenunciaResponse(
            denuncia.Id,
            denuncia.PedidoId,
            denuncia.DenunciadoId,
            denuncia.Motivo,
            denuncia.Tipo,
            denuncia.Descricao,
            denuncia.CriadoEm,
            denuncia.Status));
    }

    [HttpGet("minhas")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<DenunciaResponse>>> MinhasAsync(CancellationToken ct)
    {
        var denuncias = await _context.Denuncias
            .AsNoTracking()
            .Where(d => d.DenuncianteId == UsuarioLogadoId)
            .OrderByDescending(d => d.CriadoEm)
            .Select(d => new DenunciaResponse(
                d.Id,
                d.PedidoId,
                d.DenunciadoId,
                d.Motivo,
                d.Tipo,
                d.Descricao,
                d.CriadoEm,
                d.Status))
            .ToListAsync(ct);

        return Ok(denuncias);
    }
}