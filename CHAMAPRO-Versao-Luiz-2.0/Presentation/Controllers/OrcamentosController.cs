using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Chamapro.Api.Controllers;

/// <summary>Orçamentos (endpoints 25 a 28).</summary>
[ApiController]
[Route("api/v1")]
[Authorize]
public class OrcamentosController : ControllerBase
{
    private readonly IOrcamentoService _service;

    public OrcamentosController(IOrcamentoService service) => _service = service;

    /// <summary>25 - Profissional (que já aceitou o pedido) envia ou reenvia o orçamento.</summary>
    [HttpPost("pedidos/{pedidoId:int}/orcamentos")]
    [Authorize(Roles = "PROFISSIONAL")]
    [ProducesResponseType(typeof(OrcamentoResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<OrcamentoResponse>> Enviar(int pedidoId, [FromBody] OrcamentoCriarRequest request)
    {
        var response = await _service.EnviarAsync(pedidoId, request);
        return Created($"/api/v1/pedidos/{pedidoId}/orcamentos", response);
    }

    /// <summary>26 - Cliente dono vê os orçamentos recebidos, com nome, nota e selo de cada profissional.</summary>
    [HttpGet("pedidos/{pedidoId:int}/orcamentos")]
    [Authorize(Roles = "CLIENTE")]
    public async Task<ActionResult<IReadOnlyList<OrcamentoComProfissionalResponse>>> Listar(int pedidoId)
        => Ok(await _service.ListarDoPedidoAsync(pedidoId));

    /// <summary>27 - Cliente escolhe um orçamento (transação única).</summary>
    [HttpPost("orcamentos/{id:int}/aceitar")]
    [Authorize(Roles = "CLIENTE")]
    public async Task<ActionResult<PedidoDetalheResponse>> Aceitar(int id)
        => Ok(await _service.AceitarAsync(id));

    /// <summary>28 - Cliente recusa um orçamento; o pedido continua PENDENTE.</summary>
    [HttpPost("orcamentos/{id:int}/recusar")]
    [Authorize(Roles = "CLIENTE")]
    public async Task<IActionResult> Recusar(int id)
    {
        await _service.RecusarAsync(id);
        return NoContent();
    }
}
