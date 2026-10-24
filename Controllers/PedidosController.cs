using Chamapro.Api.Common;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Chamapro.Api.Controllers;

/// <summary>Central de solicitações (endpoints 18 a 24). Sem regra de negócio: recebe, chama o Service e devolve o HTTP certo.</summary>
[ApiController]
[Route("api/v1/pedidos")]
[Authorize]
public class PedidosController : ControllerBase
{
    private readonly IPedidoService _service;

    public PedidosController(IPedidoService service) => _service = service;

    /// <summary>18 - Cria o pedido e envia a 1 a 5 profissionais.</summary>
    [HttpPost]
    [Authorize(Roles = "CLIENTE")]
    [ProducesResponseType(typeof(PedidoCriadoResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<PedidoCriadoResponse>> Criar([FromBody] PedidoCriarRequest request)
    {
        var response = await _service.CriarAsync(request);
        return CreatedAtAction(nameof(Obter), new { id = response.Id }, response);
    }

    /// <summary>19 - Lista os pedidos criados (cliente) ou recebidos (profissional).</summary>
    [HttpGet]
    [Authorize(Roles = "CLIENTE,PROFISSIONAL")]
    public async Task<ActionResult<PagedResponse<PedidoResumoResponse>>> Listar([FromQuery] PedidoListarQuery query)
        => Ok(await _service.ListarAsync(query));

    /// <summary>20 - Detalhe do pedido (cliente dono ou profissional destinatário).</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "CLIENTE,PROFISSIONAL")]
    public async Task<ActionResult<PedidoDetalheResponse>> Obter(int id)
        => Ok(await _service.ObterAsync(id));

    /// <summary>21 - Profissional aceita atender o pedido (libera o envio de orçamento).</summary>
    [HttpPost("{id:int}/aceitar")]
    [Authorize(Roles = "PROFISSIONAL")]
    public async Task<IActionResult> Aceitar(int id)
    {
        await _service.AceitarAsync(id);
        return NoContent();
    }

    /// <summary>22 - Profissional recusa o pedido (motivo opcional).</summary>
    [HttpPost("{id:int}/recusar")]
    [Authorize(Roles = "PROFISSIONAL")]
    public async Task<IActionResult> Recusar(
        int id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] PedidoRecusarRequest? request)
    {
        await _service.RecusarAsync(id, request);
        return NoContent();
    }

    /// <summary>23 - Cliente dono ou profissional escolhido cancela o pedido.</summary>
    [HttpPost("{id:int}/cancelar")]
    [Authorize(Roles = "CLIENTE,PROFISSIONAL")]
    public async Task<IActionResult> Cancelar(int id, [FromBody] PedidoCancelarRequest request)
    {
        await _service.CancelarAsync(id, request);
        return NoContent();
    }

    /// <summary>24 - Profissional escolhido marca EM_ANDAMENTO ou CONCLUIDO.</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "PROFISSIONAL")]
    public async Task<ActionResult<PedidoDetalheResponse>> AtualizarStatus(int id, [FromBody] PedidoStatusRequest request)
        => Ok(await _service.AtualizarStatusAsync(id, request));
}
