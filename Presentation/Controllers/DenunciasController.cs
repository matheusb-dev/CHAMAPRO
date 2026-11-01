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

namespace Chamapro.Api.Controllers;

[Route(ApiRotas.Prefixo + "/denuncias")]
public class DenunciasController : ApiControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly INotificacaoService _notificacoes;
    private readonly IAuditoriaAdminService _auditoria;

    public DenunciasController(AppDbContext context, ICurrentUser currentUser, INotificacaoService notificacoes,
        IAuditoriaAdminService auditoria)
    {
        _context = context;
        _currentUser = currentUser;
        _notificacoes = notificacoes;
        _auditoria = auditoria;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<DenunciaResponse>> Criar([FromBody] DenunciaCriarRequest request, CancellationToken ct)
    {
        var denuncianteId = _currentUser.Id;
        var denunciante = await _context.Usuarios.AnyAsync(u => u.Id == denuncianteId, ct);
        if (!denunciante)
            throw BusinessException.NaoEncontrado("USUARIO_DENUNCIANTE_NAO_ENCONTRADO", "Usuário denunciante não encontrado.");

        var tipoEntidade = request.TipoEntidade.Trim().ToUpperInvariant();
        int? denunciadoId = null;
        var entidadeValida = false;
        switch (tipoEntidade)
        {
            case "PROFISSIONAL":
                denunciadoId = await _context.Profissionais.AsNoTracking()
                    .Where(p => p.Id == request.EntidadeId).Select(p => (int?)p.UsuarioId).FirstOrDefaultAsync(ct);
                entidadeValida = denunciadoId.HasValue;
                break;
            case "USUARIO":
                denunciadoId = request.EntidadeId;
                entidadeValida = true;
                break;
            case "AVALIACAO":
                denunciadoId = await _context.Avaliacoes.AsNoTracking()
                    .Where(a => a.Id == request.EntidadeId).Select(a => (int?)a.AvaliadoId).FirstOrDefaultAsync(ct);
                entidadeValida = denunciadoId.HasValue;
                break;
            case "PEDIDO":
                denunciadoId = request.DenunciadoId;
                entidadeValida = denunciadoId.HasValue && await DenunciaPedidoValidaAsync(
                    request.EntidadeId, denuncianteId, denunciadoId.Value, ct);
                break;
        }
        if (request.DenunciadoId.HasValue && request.DenunciadoId != denunciadoId)
            entidadeValida = false;
        if (!entidadeValida)
            throw BusinessException.Invalido("ENTIDADE_NAO_RELACIONADA", "O usuário denunciado não corresponde ao perfil, chamado ou avaliação informados.");
        if (denunciadoId is null || denunciadoId <= 0)
            throw BusinessException.Invalido("DENUNCIADO_INVALIDO", "Usuário denunciado inválido.");
        if (denuncianteId == denunciadoId)
            throw BusinessException.Invalido("DENUNCIA_PROPRIA", "Você não pode denunciar a si mesmo.");

        var denunciado = await _context.Usuarios.AnyAsync(u => u.Id == denunciadoId, ct);
        if (!denunciado)
            throw BusinessException.NaoEncontrado("USUARIO_DENUNCIADO_NAO_ENCONTRADO", "Usuário denunciado não encontrado.");

        var duplicada = await _context.Denuncias.AnyAsync(d =>
            d.DenuncianteId == denuncianteId && d.DenunciadoId == denunciadoId
            && d.TipoEntidade == tipoEntidade && d.EntidadeId == request.EntidadeId
            && (d.Status == StatusDenuncia.Pendente || d.Status == StatusDenuncia.EmAnalise), ct);
        if (duplicada)
            throw BusinessException.Conflito("DENUNCIA_DUPLICADA", "Já existe uma denúncia em análise para este item.");

        var denuncia = new Denuncia
        {
            DenuncianteId = denuncianteId,
            DenunciadoId = denunciadoId.Value,
            TipoEntidade = tipoEntidade,
            EntidadeId = request.EntidadeId,
            Motivo = request.Motivo.Trim(),
            Descricao = request.Descricao.Trim(),
            Evidencias = request.Evidencias?.Trim(),
            Status = StatusDenuncia.Pendente,
            CriadoEm = DateTime.UtcNow
        };

        await _context.Denuncias.AddAsync(denuncia, ct);
        await _context.SaveChangesAsync(ct);

        return Ok(new DenunciaResponse
        {
            Id = denuncia.Id,
            DenuncianteId = denuncia.DenuncianteId,
            DenunciadoId = denuncia.DenunciadoId,
            TipoEntidade = denuncia.TipoEntidade,
            EntidadeId = denuncia.EntidadeId,
            Motivo = denuncia.Motivo,
            Descricao = denuncia.Descricao,
            Evidencias = denuncia.Evidencias,
            Status = denuncia.Status.ToString(),
            ObservacaoAdmin = denuncia.ObservacaoAdmin,
            CriadoEm = denuncia.CriadoEm
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<List<DenunciaResponse>>> MinhasDenuncias(CancellationToken ct)
    {
        var denuncias = await _context.Denuncias
            .Where(d => d.DenuncianteId == _currentUser.Id)
            .OrderByDescending(d => d.CriadoEm)
            .Select(d => new DenunciaResponse
            {
                Id = d.Id,
                DenuncianteId = d.DenuncianteId,
                DenunciadoId = d.DenunciadoId,
                TipoEntidade = d.TipoEntidade,
                EntidadeId = d.EntidadeId,
                Motivo = d.Motivo,
                Descricao = d.Descricao,
                Evidencias = d.Evidencias,
                Status = d.Status.ToString(),
                ObservacaoAdmin = d.ObservacaoAdmin,
                CriadoEm = d.CriadoEm
            })
            .ToListAsync(ct);

        return Ok(denuncias);
    }

    [HttpGet]
    [Authorize(Roles = Perfis.Admin)]
    public async Task<ActionResult<List<DenunciaResponse>>> ListarPendentes(CancellationToken ct)
    {
        var denuncias = await _context.Denuncias
            .Where(d => d.Status == StatusDenuncia.Pendente || d.Status == StatusDenuncia.EmAnalise)
            .OrderBy(d => d.CriadoEm)
            .Select(d => new DenunciaResponse
            {
                Id = d.Id,
                DenuncianteId = d.DenuncianteId,
                DenunciadoId = d.DenunciadoId,
                TipoEntidade = d.TipoEntidade,
                EntidadeId = d.EntidadeId,
                Motivo = d.Motivo,
                Descricao = d.Descricao,
                Evidencias = d.Evidencias,
                Status = d.Status.ToString(),
                ObservacaoAdmin = d.ObservacaoAdmin,
                CriadoEm = d.CriadoEm
            })
            .ToListAsync(ct);

        return Ok(denuncias);
    }

    [HttpPatch("{id:int}/resolver")]
    [Authorize(Roles = Perfis.Admin)]
    public async Task<ActionResult<DenunciaResponse>> Resolver(
        int id,
        [FromBody] DenunciaAvaliacaoRequest request,
        CancellationToken ct)
    {
        var denuncia = await _context.Denuncias.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (denuncia is null)
            throw BusinessException.NaoEncontrado("DENUNCIA_NAO_ENCONTRADA", "Denúncia não encontrada.");

        if (!Enum.TryParse<StatusDenuncia>(request.Status, true, out var novoStatus)
            || !Enum.IsDefined(novoStatus) || novoStatus == StatusDenuncia.Pendente)
            throw BusinessException.Invalido("STATUS_INVALIDO", "Use EmAnalise, Resolvido ou Rejeitado.");

        var statusAnterior = denuncia.Status;
        denuncia.Status = novoStatus;
        denuncia.ObservacaoAdmin = request.Observacao ?? denuncia.ObservacaoAdmin;
        denuncia.AtualizadoEm = DateTime.UtcNow;
        _notificacoes.Adicionar(denuncia.DenuncianteId, "denuncia_atualizada", "Sua denúncia foi atualizada",
            $"A denúncia #{denuncia.Id} foi marcada como {novoStatus}.", new { denunciaId = denuncia.Id, status = novoStatus.ToString() });
        if (statusAnterior != novoStatus)
            _auditoria.Registrar(_currentUser.Id, "DENUNCIA_RESOLVIDA", "DENUNCIA", denuncia.Id,
                statusAnterior.ToString(), novoStatus.ToString());

        await _context.SaveChangesAsync(ct);

        return Ok(new DenunciaResponse
        {
            Id = denuncia.Id,
            DenuncianteId = denuncia.DenuncianteId,
            DenunciadoId = denuncia.DenunciadoId,
            TipoEntidade = denuncia.TipoEntidade,
            EntidadeId = denuncia.EntidadeId,
            Motivo = denuncia.Motivo,
            Descricao = denuncia.Descricao,
            Evidencias = denuncia.Evidencias,
            Status = denuncia.Status.ToString(),
            ObservacaoAdmin = denuncia.ObservacaoAdmin,
            CriadoEm = denuncia.CriadoEm
        });
    }

    private async Task<bool> DenunciaPedidoValidaAsync(int pedidoId, int denuncianteId, int denunciadoId, CancellationToken ct)
    {
        var pedido = await _context.Pedidos.AsNoTracking()
            .Where(p => p.Id == pedidoId)
            .Select(p => new { p.ClienteId, p.ProfissionalId })
            .FirstOrDefaultAsync(ct);
        if (pedido is null) return false;

        if (pedido.ClienteId == denuncianteId)
        {
            return await (from destinatario in _context.PedidoDestinatarios.AsNoTracking()
                          join profissional in _context.Profissionais.AsNoTracking()
                              on destinatario.ProfissionalId equals profissional.Id
                          where destinatario.PedidoId == pedidoId && profissional.UsuarioId == denunciadoId
                          select destinatario.PedidoId).AnyAsync(ct)
                || await _context.Profissionais.AsNoTracking()
                    .AnyAsync(p => p.Id == pedido.ProfissionalId && p.UsuarioId == denunciadoId, ct);
        }

        var profissionalId = await _context.Profissionais.AsNoTracking()
            .Where(p => p.UsuarioId == denuncianteId)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(ct);
        return profissionalId is not null
            && (pedido.ProfissionalId == profissionalId.Value
                || await _context.PedidoDestinatarios.AsNoTracking()
                    .AnyAsync(d => d.PedidoId == pedidoId && d.ProfissionalId == profissionalId.Value, ct))
            && pedido.ClienteId == denunciadoId;
    }
}
