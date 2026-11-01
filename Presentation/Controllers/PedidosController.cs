using Chamapro.Api.Common;
using Chamapro.Api.Data;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Chamapro.Api.Repositories;
using Chamapro.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Controllers;

[Route(ApiRotas.Prefixo + "/pedidos")]
[Authorize]
public class PedidosController : ApiControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPedidoRepository _pedidoRepository;

    public PedidosController(AppDbContext context, IPedidoRepository pedidoRepository)
    {
        _context = context;
        _pedidoRepository = pedidoRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PedidoResumoResponse>>> ListarAsync(
        [FromQuery] int? clienteId,
        [FromQuery] int? profissionalId,
        [FromQuery] StatusPedido? status,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 10,
        CancellationToken ct = default)
    {
        var (itens, total) = await _pedidoRepository.ListarAsync(clienteId, profissionalId, status, pagina, tamanho);

        var response = itens
            .Select(p => p.ToResumo(profissionalId))
            .ToList();

        return Ok(new { itens = response, total });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PedidoDetalheResponse>> ObterAsync(int id, CancellationToken ct)
    {
        var pedido = await _pedidoRepository.ObterDetalheAsync(id);

        if (pedido is null)
            return NotFound();

        var ehCliente = pedido.ClienteId == UsuarioLogadoId;
        var profissionalId = await _pedidoRepository.ObterProfissionalIdPorUsuarioAsync(UsuarioLogadoId);

        return Ok(pedido.ToDetalhe(ehCliente, profissionalId));
    }
}