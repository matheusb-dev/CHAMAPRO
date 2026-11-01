using Chamapro.Api.Common;
using Chamapro.Api.Data;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Chamapro.Api.Models.Time;
using Chamapro.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Chamapro.Api.Controllers;

[ApiController]
[Route(ApiRotas.Prefixo + "/agendamentos")]
[Authorize(Roles = "CLIENTE,PROFISSIONAL,ADMIN")]
public class AgendamentosController : ApiControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly INotificacaoService _notificacoes;

    public AgendamentosController(AppDbContext context, ICurrentUser currentUser, INotificacaoService notificacoes)
    {
        _context = context;
        _currentUser = currentUser;
        _notificacoes = notificacoes;
    }

    [HttpPost]
    public async Task<ActionResult<AgendamentoResponse>> Criar(
        [FromBody] AgendamentoCriarRequest request,
        CancellationToken ct)
    {
        if (request.PedidoId <= 0)
            throw BusinessException.Invalido("PEDIDO_INVALIDO", "Pedido inválido.");
        ValidarDataFutura(request.DataHora);

        var pedido = await _context.Pedidos
            .FirstOrDefaultAsync(x => x.Id == request.PedidoId, ct);
        if (pedido is null)
            throw BusinessException.NaoEncontrado("PEDIDO_NAO_ENCONTRADO", "Pedido não encontrado.");
        await ExigirParticipacaoAsync(pedido, ct);

        if (pedido.Status != StatusPedido.Aceito || pedido.ProfissionalId is null)
            throw BusinessException.Conflito("PEDIDO_NAO_PRONTO_PARA_AGENDAR", "O pedido precisa ter um orçamento aceito antes do agendamento.");

        if (await _context.Agendamentos.AnyAsync(x => x.PedidoId == request.PedidoId, ct))
            throw BusinessException.Conflito("AGENDAMENTO_JA_EXISTE", "Este pedido já possui agendamento.");
        await ExigirHorarioLivreAsync(pedido.ProfissionalId.Value, request.DataHora, null, ct);

        var statusAnterior = pedido.Status;
        var agendamento = new Agendamento
        {
            PedidoId = pedido.Id,
            ProfissionalId = pedido.ProfissionalId.Value,
            DataHora = request.DataHora,
            Status = StatusAgendamento.Agendado
        };
        pedido.Status = StatusPedido.Agendado;
        _context.HistoricosPedidos.Add(new HistoricoPedido
        {
            PedidoId = pedido.Id, AtorId = _currentUser.Id, StatusAnterior = statusAnterior.ToString(),
            StatusNovo = pedido.Status.ToString(), Detalhe = "Agendamento criado.", CriadoEm = DateTime.UtcNow
        });
        await _context.Agendamentos.AddAsync(agendamento, ct);
        await NotificarOutroParticipanteAsync(pedido, "agendamento_confirmado", "Agendamento solicitado",
            "Um participante criou o agendamento do serviço.", ct);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg
            && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw BusinessException.Conflito("HORARIO_INDISPONIVEL", "Outro agendamento ocupou este pedido ou horário.");
        }

        return CreatedAtAction(nameof(ObterPorPedido), new { pedidoId = pedido.Id }, ParaResponse(agendamento));
    }

    [HttpGet("{pedidoId:int}")]
    public async Task<ActionResult<AgendamentoResponse>> ObterPorPedido(int pedidoId, CancellationToken ct)
    {
        var pedido = await _context.Pedidos.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == pedidoId, ct);
        if (pedido is null)
            throw BusinessException.NaoEncontrado("PEDIDO_NAO_ENCONTRADO", "Pedido não encontrado.");
        await ExigirParticipacaoAsync(pedido, ct);

        var agendamento = await _context.Agendamentos.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PedidoId == pedidoId, ct);
        if (agendamento is null)
            throw BusinessException.NaoEncontrado("AGENDAMENTO_NAO_ENCONTRADO", "Agendamento não encontrado.");

        return Ok(ParaResponse(agendamento));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AgendamentoResponse>> Remarcar(
        int id, [FromBody] AgendamentoRemarcarRequest request, CancellationToken ct)
    {
        ValidarDataFutura(request.DataHora);
        var agendamento = await _context.Agendamentos
            .Include(a => a.Pedido)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (agendamento is null)
            throw BusinessException.NaoEncontrado("AGENDAMENTO_NAO_ENCONTRADO", "Agendamento não encontrado.");
        await ExigirParticipacaoAsync(agendamento.Pedido, ct);

        if (agendamento.Status is StatusAgendamento.Cancelado or StatusAgendamento.Concluido)
            throw BusinessException.Conflito("AGENDAMENTO_ENCERRADO", "Um agendamento encerrado não pode ser remarcado.");
        if (agendamento.Pedido.ProfissionalId is null)
            throw BusinessException.Conflito("PROFISSIONAL_NAO_SELECIONADO", "O pedido ainda não tem profissional selecionado.");

        await ExigirHorarioLivreAsync(agendamento.Pedido.ProfissionalId.Value, request.DataHora, id, ct);
        agendamento.DataHora = request.DataHora;
        agendamento.LembreteEnviadoEm = null;
        agendamento.Status = StatusAgendamento.Agendado;
        _context.HistoricosPedidos.Add(new HistoricoPedido
        {
            PedidoId = agendamento.PedidoId, AtorId = _currentUser.Id,
            StatusAnterior = agendamento.Pedido.Status.ToString(), StatusNovo = agendamento.Pedido.Status.ToString(),
            Detalhe = "Agendamento remarcado.", CriadoEm = DateTime.UtcNow
        });
        await NotificarOutroParticipanteAsync(agendamento.Pedido, "agendamento_alterado", "Agendamento alterado",
            "A data do serviço foi alterada.", ct);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg
            && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw BusinessException.Conflito("HORARIO_INDISPONIVEL", "Outro agendamento ocupou este horário.");
        }
        return Ok(ParaResponse(agendamento));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<AgendamentoResponse>> AtualizarStatus(
        int id, [FromBody] AgendamentoStatusRequest request, CancellationToken ct)
    {
        var agendamento = await _context.Agendamentos
            .Include(a => a.Pedido)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (agendamento is null)
            throw BusinessException.NaoEncontrado("AGENDAMENTO_NAO_ENCONTRADO", "Agendamento não encontrado.");
        await ExigirParticipacaoAsync(agendamento.Pedido, ct);

        if (!Enum.TryParse<StatusAgendamento>(request.Status, true, out var novoStatus)
            || !Enum.IsDefined(novoStatus))
            throw BusinessException.Invalido("STATUS_INVALIDO", "Status inválido.");
        if (agendamento.Status is StatusAgendamento.Cancelado or StatusAgendamento.Concluido)
            throw BusinessException.Conflito("AGENDAMENTO_ENCERRADO", "Um agendamento encerrado não pode ser alterado.");

        var transicaoValida = (agendamento.Status == StatusAgendamento.Agendado
                && novoStatus is StatusAgendamento.Confirmado or StatusAgendamento.Cancelado or StatusAgendamento.Concluido)
            || (agendamento.Status == StatusAgendamento.Confirmado
                && novoStatus is StatusAgendamento.Cancelado or StatusAgendamento.Concluido);
        if (!transicaoValida)
            throw BusinessException.Conflito("TRANSICAO_INVALIDA", "A mudança de status do agendamento não é permitida.");

        if (_currentUser.EhCliente && novoStatus != StatusAgendamento.Cancelado)
            throw BusinessException.Proibido("STATUS_RESTRITO", "O cliente pode cancelar o agendamento; confirmação e conclusão pertencem ao profissional.");
        if (_currentUser.EhProfissional && novoStatus is not (StatusAgendamento.Confirmado or StatusAgendamento.Concluido))
            throw BusinessException.Proibido("STATUS_RESTRITO", "O profissional pode confirmar ou concluir o agendamento.");
        if (novoStatus == StatusAgendamento.Confirmado && agendamento.Status != StatusAgendamento.Agendado)
            throw BusinessException.Conflito("TRANSICAO_INVALIDA", "Somente um agendamento aguardando confirmação pode ser confirmado.");

        var statusAnteriorPedido = agendamento.Pedido.Status;
        agendamento.Status = novoStatus;
        if (novoStatus == StatusAgendamento.Concluido)
            agendamento.Pedido.Status = StatusPedido.Concluido;
        else if (novoStatus == StatusAgendamento.Cancelado)
        {
            var antecedencia = agendamento.DataHora - DateTime.UtcNow;
            agendamento.Pedido.CancelamentoTardio = antecedencia >= TimeSpan.Zero
                && antecedencia <= TimeSpan.FromHours(24);
            agendamento.Pedido.Status = StatusPedido.Cancelado;
            agendamento.Pedido.CanceladoPorId = _currentUser.Id;
            agendamento.Pedido.CanceladoEm = DateTime.UtcNow;
            agendamento.Pedido.MotivoCancelamento = "Agendamento cancelado.";
        }

        await NotificarOutroParticipanteAsync(agendamento.Pedido,
            novoStatus == StatusAgendamento.Concluido ? "servico_concluido" :
            novoStatus == StatusAgendamento.Cancelado ? "agendamento_cancelado" : "agendamento_confirmado",
            novoStatus == StatusAgendamento.Concluido ? "Serviço concluído" :
            novoStatus == StatusAgendamento.Cancelado ? "Agendamento cancelado" : "Agendamento confirmado",
            novoStatus == StatusAgendamento.Concluido ? "O serviço foi marcado como concluído." :
            novoStatus == StatusAgendamento.Cancelado ? "O agendamento foi cancelado." : "O profissional confirmou o agendamento.", ct);

        _context.HistoricosPedidos.Add(new HistoricoPedido
        {
            PedidoId = agendamento.PedidoId, AtorId = _currentUser.Id,
            StatusAnterior = statusAnteriorPedido.ToString(), StatusNovo = agendamento.Pedido.Status.ToString(),
            Detalhe = $"Status do agendamento atualizado para {novoStatus}.", CriadoEm = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);
        return Ok(ParaResponse(agendamento));
    }

    private async Task ExigirParticipacaoAsync(Pedido pedido, CancellationToken ct)
    {
        if (_currentUser.EhAdmin) return;
        if (_currentUser.EhCliente && pedido.ClienteId == _currentUser.Id) return;
        if (_currentUser.EhProfissional && pedido.ProfissionalId is int profissionalId
            && await _context.Profissionais.AsNoTracking()
                .AnyAsync(p => p.Id == profissionalId && p.UsuarioId == _currentUser.Id, ct))
            return;

        throw BusinessException.Proibido("PEDIDO_FORA_DO_ESCOPO", "Você não participa deste pedido.");
    }

    private async Task NotificarOutroParticipanteAsync(Pedido pedido, string tipo, string titulo, string mensagem, CancellationToken ct)
    {
        if (_currentUser.EhAdmin) return;
        int destinoId;
        if (_currentUser.EhCliente)
        {
            if (pedido.ProfissionalId is null) return;
            destinoId = await _context.Profissionais.AsNoTracking()
                .Where(p => p.Id == pedido.ProfissionalId.Value)
                .Select(p => p.UsuarioId).FirstAsync(ct);
        }
        else
        {
            destinoId = pedido.ClienteId;
        }
        _notificacoes.Adicionar(destinoId, tipo, titulo, mensagem, new { pedidoId = pedido.Id });
    }

    private async Task ExigirHorarioLivreAsync(int profissionalId, DateTime dataHora, int? ignorarAgendamentoId, CancellationToken ct)
    {
        var local = HorarioBrasil.ParaLocal(dataHora);
        var dia = (short)local.DayOfWeek;
        var hora = TimeOnly.FromDateTime(local);
        var dentroDaDisponibilidade = await _context.DisponibilidadesProfissionais.AsNoTracking()
            .AnyAsync(d => d.ProfissionalId == profissionalId && d.DiaSemana == dia && d.AtendeAgendado
                && d.HoraInicio <= hora && d.HoraFim > hora, ct);
        if (!dentroDaDisponibilidade)
            throw BusinessException.Regra("FORA_DA_DISPONIBILIDADE", "O horário escolhido não está na disponibilidade agendada do profissional.");

        var conflito = await _context.Agendamentos.AsNoTracking()
            .AnyAsync(a => a.ProfissionalId == profissionalId
                && a.DataHora == dataHora
                && a.Status != StatusAgendamento.Cancelado
                && a.Id != ignorarAgendamentoId, ct);
        if (conflito)
            throw BusinessException.Conflito("HORARIO_INDISPONIVEL", "O profissional já possui um agendamento nesse horário.");
    }

    private static void ValidarDataFutura(DateTime dataHora)
    {
        if (dataHora <= DateTime.UtcNow)
            throw BusinessException.Invalido("DATA_INVALIDA", "A data do agendamento deve ser futura.");
    }

    private static AgendamentoResponse ParaResponse(Agendamento agendamento)
        => new()
        {
            Id = agendamento.Id,
            PedidoId = agendamento.PedidoId,
            DataHora = agendamento.DataHora,
            Status = agendamento.Status.ToString()
        };
}
