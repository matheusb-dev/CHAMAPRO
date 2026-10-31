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

[Route(ApiRotas.Prefixo + "/denuncias")]
public class DenunciasController : ApiControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DenunciasController(AppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<DenunciaResponse>> Criar([FromBody] DenunciaCriarRequest request, CancellationToken ct)
    {
        if (request.DenunciadoId <= 0)
            throw BusinessException.Invalido("DENUNCIADO_INVALIDO", "Usuário denunciado inválido.");

        if (string.IsNullOrWhiteSpace(request.Motivo))
            throw BusinessException.Invalido("MOTIVO_INVALIDO", "Informe o motivo da denúncia.");

        if (string.IsNullOrWhiteSpace(request.Descricao))
            throw BusinessException.Invalido("DESCRICAO_INVALIDA", "Informe a descrição do ocorrido.");

        if (string.IsNullOrWhiteSpace(request.TipoEntidade))
            throw BusinessException.Invalido("TIPO_ENTIDADE_INVALIDO", "Informe o tipo da entidade denunciada.");

        var denuncianteId = _currentUser.Id;

        if (denuncianteId == request.DenunciadoId)
            throw BusinessException.Invalido("DENUNCIA_PROPRIA", "Você não pode denunciar a si mesmo.");

        var denunciante = await _context.Usuarios.AnyAsync(u => u.Id == denuncianteId, ct);
        if (!denunciante)
            throw BusinessException.NaoEncontrado("USUARIO_DENUNCIANTE_NAO_ENCONTRADO", "Usuário denunciante não encontrado.");

        var denunciado = await _context.Usuarios.AnyAsync(u => u.Id == request.DenunciadoId, ct);
        if (!denunciado)
            throw BusinessException.NaoEncontrado("USUARIO_DENUNCIADO_NAO_ENCONTRADO", "Usuário denunciado não encontrado.");

        var denuncia = new Denuncia
        {
            DenuncianteId = denuncianteId,
            DenunciadoId = request.DenunciadoId,
            TipoEntidade = request.TipoEntidade.Trim(),
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

        if (!Enum.TryParse<StatusDenuncia>(request.Status, true, out var novoStatus))
            throw BusinessException.Invalido("STATUS_INVALIDO", "Status da denúncia inválido.");

        denuncia.Status = novoStatus;
        denuncia.ObservacaoAdmin = request.Observacao ?? denuncia.ObservacaoAdmin;
        denuncia.AtualizadoEm = DateTime.UtcNow;

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
}