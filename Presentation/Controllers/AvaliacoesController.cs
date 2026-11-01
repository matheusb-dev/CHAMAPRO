using Chamapro.Api.Common;
using Chamapro.Api.Data;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Chamapro.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Chamapro.Api.Controllers;

[ApiController]
[Route(ApiRotas.Prefixo + "/avaliacoes")]
[Authorize(Roles = Perfis.Cliente)]
public sealed class AvaliacoesController : ApiControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;
    private readonly INotificacaoService _notificacoes;

    public AvaliacoesController(AppDbContext db, ICurrentUser user, INotificacaoService notificacoes)
    {
        _db = db;
        _user = user;
        _notificacoes = notificacoes;
    }

    [HttpGet("pendentes")]
    public async Task<ActionResult<IReadOnlyList<AvaliacaoPendenteResponse>>> ListarPendentes(CancellationToken ct)
    {
        var itens = await _db.Pedidos.AsNoTracking()
            .Where(p => p.ClienteId == _user.Id && p.Status == StatusPedido.Concluido
                && p.ProfissionalId != null && p.Avaliacao == null)
            .OrderByDescending(p => p.CriadoEm)
            .Select(p => new AvaliacaoPendenteResponse(
                p.Id, p.Titulo, p.ProfissionalId!.Value, p.Profissional!.Usuario.Nome,
                p.Agendamento != null ? p.Agendamento.DataHora : p.CriadoEm))
            .ToListAsync(ct);
        return Ok(itens);
    }

    [HttpPost]
    public async Task<ActionResult<AvaliacaoCriadaResponse>> Criar([FromBody] AvaliacaoCriarRequest request, CancellationToken ct)
    {
        var pedido = await _db.Pedidos.Include(p => p.Avaliacao)
            .FirstOrDefaultAsync(p => p.Id == request.PedidoId && p.ClienteId == _user.Id, ct);
        if (pedido is null)
            throw BusinessException.NaoEncontrado("PEDIDO_NAO_ENCONTRADO", "Pedido não encontrado.");
        if (pedido.Status != StatusPedido.Concluido || pedido.ProfissionalId is null)
            throw BusinessException.Regra("SERVICO_NAO_CONCLUIDO", "A avaliação só pode ser enviada para um serviço concluído.");
        if (pedido.Avaliacao is not null)
            throw BusinessException.Conflito("AVALIACAO_DUPLICADA", "Este serviço já foi avaliado.");

        var profissional = await _db.Profissionais.FirstOrDefaultAsync(p => p.Id == pedido.ProfissionalId.Value, ct)
            ?? throw BusinessException.Regra("PROFISSIONAL_NAO_ENCONTRADO", "O profissional deste chamado não foi encontrado.");
        var avaliacao = new Avaliacao
        {
            PedidoId = pedido.Id,
            AvaliadorId = _user.Id,
            AvaliadoId = profissional.UsuarioId,
            Nota = request.Nota,
            Comentario = string.IsNullOrWhiteSpace(request.Comentario) ? null : request.Comentario.Trim(),
            CriadoEm = DateTime.UtcNow
        };

        await _db.Avaliacoes.AddAsync(avaliacao, ct);
        var resumo = await _db.Avaliacoes.AsNoTracking()
            .Where(a => a.AvaliadoId == profissional.UsuarioId)
            .GroupBy(a => a.AvaliadoId)
            .Select(g => new { Media = g.Average(a => a.Nota), Total = g.Count() })
            .FirstOrDefaultAsync(ct);
        var totalAnterior = resumo?.Total ?? 0;
        profissional.MediaAvaliacao = Math.Round(
            (decimal)(((resumo?.Media ?? 0d) * totalAnterior + request.Nota) / (totalAnterior + 1)), 1);
        _notificacoes.Adicionar(profissional.UsuarioId, "nova_avaliacao", "Você recebeu uma avaliação",
            "Um cliente avaliou o serviço concluído.", new { pedidoId = pedido.Id, nota = request.Nota });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg
            && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw BusinessException.Conflito("AVALIACAO_DUPLICADA", "Este serviço já foi avaliado.");
        }

        var resposta = new AvaliacaoCriadaResponse(
            avaliacao.Id, avaliacao.PedidoId, profissional.Id, avaliacao.Nota, avaliacao.Comentario, avaliacao.CriadoEm);
        return Created($"/{ApiRotas.Prefixo}/pedidos/{pedido.Id}", resposta);
    }
}
