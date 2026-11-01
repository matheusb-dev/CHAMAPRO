using Chamapro.Api.Common;
using Chamapro.Api.Data;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Enums;
using Chamapro.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Controllers;

[ApiController]
[Route(ApiRotas.Prefixo + "/painel")]
[Authorize(Roles = Perfis.Profissional)]
public sealed class PainelProfissionalController : ApiControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;

    public PainelProfissionalController(AppDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    [HttpGet("profissional")]
    public async Task<ActionResult<PainelProfissionalResponse>> Obter(CancellationToken ct)
    {
        var profissional = await _db.Profissionais.AsNoTracking()
            .Where(p => p.UsuarioId == _user.Id)
            .Select(p => new { p.Id, p.MediaAvaliacao })
            .FirstOrDefaultAsync(ct)
            ?? throw BusinessException.NaoEncontrado("PERFIL_NAO_ENCONTRADO", "Perfil profissional não encontrado.");

        var destinatarios = from d in _db.PedidoDestinatarios.AsNoTracking()
                            join p in _db.Pedidos.AsNoTracking() on d.PedidoId equals p.Id
                            where d.ProfissionalId == profissional.Id
                            select new { d.Situacao, p.Status };

        var novos = await destinatarios.CountAsync(x => x.Situacao == SituacaoDestinatario.Pendente
            && x.Status == StatusPedido.Pendente, ct);
        var aceitos = await destinatarios.CountAsync(x => x.Situacao == SituacaoDestinatario.Aceito
            && x.Status == StatusPedido.Pendente, ct);
        var orcamentos = await _db.Orcamentos.AsNoTracking().CountAsync(o => o.ProfissionalId == profissional.Id, ct);
        var agendamentos = await _db.Agendamentos.AsNoTracking().CountAsync(a => a.ProfissionalId == profissional.Id
            && a.DataHora >= DateTime.UtcNow
            && (a.Status == StatusAgendamento.Agendado || a.Status == StatusAgendamento.Confirmado), ct);
        var emAndamento = await _db.Pedidos.AsNoTracking().CountAsync(p => p.ProfissionalId == profissional.Id
            && p.Status == StatusPedido.EmAndamento, ct);
        var concluidos = await _db.Pedidos.AsNoTracking().CountAsync(p => p.ProfissionalId == profissional.Id
            && p.Status == StatusPedido.Concluido, ct);

        var statusVerificacao = await _db.Verificacoes.AsNoTracking()
            .Where(v => v.ProfissionalId == profissional.Id)
            .OrderByDescending(v => v.CriadoEm)
            .Select(v => v.Status.ToString())
            .FirstOrDefaultAsync(ct) ?? "CadastroIncompleto";

        return Ok(new PainelProfissionalResponse(novos, aceitos, orcamentos, agendamentos,
            emAndamento, concluidos, profissional.MediaAvaliacao, statusVerificacao));
    }
}
