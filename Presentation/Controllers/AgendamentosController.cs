using Chamapro.Api.Common;
using Chamapro.Api.Data;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Controllers;

[ApiController]
[Route(ApiRotas.Prefixo + "/agendamentos")]
[Authorize]
public class AgendamentosController : ApiControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public AgendamentosController(AppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<ActionResult<AgendamentoResponse>> Criar(
        [FromBody] AgendamentoCriarRequest request,
        CancellationToken ct)
    {
        if (request.PedidoId <= 0)
            throw BusinessException.Invalido("PEDIDO_INVALIDO", "Pedido inválido.");

        if (request.DataHora <= DateTime.UtcNow)
            throw BusinessException.Invalido("DATA_INVALIDA", "A data do agendamento deve ser futura.");

        var pedido = await _context.Pedidos
            .FirstOrDefaultAsync(x => x.Id == request.PedidoId, ct);

        if (pedido is null)
            throw BusinessException.NaoEncontrado("PEDIDO_NAO_ENCONTRADO", "Pedido não encontrado.");

        var jaExiste = await _context.Agendamentos
            .AnyAsync(x => x.PedidoId == request.PedidoId, ct);

        if (jaExiste)
            throw BusinessException.Invalido("AGENDAMENTO_JA_EXISTE", "Este pedido já possui agendamento.");

        var agendamento = new Agendamento
        {
            PedidoId = request.PedidoId,
            DataHora = request.DataHora,
            Status = StatusAgendamento.Agendado
        };

        await _context.Agendamentos.AddAsync(agendamento, ct);
        await _context.SaveChangesAsync(ct);

        return Ok(new AgendamentoResponse
        {
            Id = agendamento.Id,
            PedidoId = agendamento.PedidoId,
            DataHora = agendamento.DataHora,
            Status = agendamento.Status.ToString()
        });
    }

    [HttpGet("{pedidoId:int}")]
    public async Task<ActionResult<AgendamentoResponse>> ObterPorPedido(int pedidoId, CancellationToken ct)
    {
        var agendamento = await _context.Agendamentos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PedidoId == pedidoId, ct);

        if (agendamento is null)
            throw BusinessException.NaoEncontrado("AGENDAMENTO_NAO_ENCONTRADO", "Agendamento não encontrado.");

        return Ok(new AgendamentoResponse
        {
            Id = agendamento.Id,
            PedidoId = agendamento.PedidoId,
            DataHora = agendamento.DataHora,
            Status = agendamento.Status.ToString()
        });
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<AgendamentoResponse>> AtualizarStatus(
        int id,
        [FromBody] AgendamentoStatusRequest request,
        CancellationToken ct)
    {
        var agendamento = await _context.Agendamentos
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (agendamento is null)
            throw BusinessException.NaoEncontrado("AGENDAMENTO_NAO_ENCONTRADO", "Agendamento não encontrado.");

        if (!Enum.TryParse<StatusAgendamento>(request.Status, true, out var novoStatus))
            throw BusinessException.Invalido("STATUS_INVALIDO", "Status inválido.");

        agendamento.Status = novoStatus;

        await _context.SaveChangesAsync(ct);

        return Ok(new AgendamentoResponse
        {
            Id = agendamento.Id,
            PedidoId = agendamento.PedidoId,
            DataHora = agendamento.DataHora,
            Status = agendamento.Status.ToString()
        });
    }
}