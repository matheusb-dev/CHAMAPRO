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

[Route(ApiRotas.Prefixo + "/verificacoes")]
public class VerificacoesController : ApiControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public VerificacoesController(AppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpPost]
    [Authorize(Roles = Perfis.Profissional)]
    public async Task<ActionResult<VerificacaoResponse>> Solicitar([FromBody] VerificacaoDocumentosRequest request, CancellationToken ct)
    {
        var profissional = await _context.Profissionais.FirstOrDefaultAsync(p => p.UsuarioId == _currentUser.Id, ct);
        if (profissional is null)
            throw BusinessException.NaoEncontrado("PERFIL_NAO_ENCONTRADO", "Perfil profissional não encontrado.");

        var verificacao = new VerificacaoProfissional
        {
            ProfissionalId = profissional.Id,
            TipoDocumento = request.TipoDocumento.Trim(),
            UrlDocumento = request.UrlDocumento.Trim(),
            Status = StatusVerificacao.Pendente,
            Observacao = request.Observacao,
            CriadoEm = DateTime.UtcNow
        };

        await _context.Verificacoes.AddAsync(verificacao, ct);
        await _context.SaveChangesAsync(ct);

        return Ok(new VerificacaoResponse
        {
            Id = verificacao.Id,
            ProfissionalId = profissional.Id,
            TipoDocumento = verificacao.TipoDocumento,
            UrlDocumento = verificacao.UrlDocumento,
            Status = verificacao.Status.ToString(),
            CriadoEm = verificacao.CriadoEm,
            Observacao = verificacao.Observacao
        });
    }

    [HttpGet]
    [Authorize(Roles = Perfis.Admin)]
    public async Task<ActionResult<List<VerificacaoResponse>>> ListarPendentes(CancellationToken ct)
    {
        var itens = await _context.Verificacoes
            .Where(v => v.Status == StatusVerificacao.Pendente)
            .OrderBy(v => v.CriadoEm)
            .Select(v => new VerificacaoResponse
            {
                Id = v.Id,
                ProfissionalId = v.ProfissionalId,
                TipoDocumento = v.TipoDocumento,
                UrlDocumento = v.UrlDocumento,
                Status = v.Status.ToString(),
                CriadoEm = v.CriadoEm,
                Observacao = v.Observacao
            })
            .ToListAsync(ct);

        return Ok(itens);
    }

    [HttpPatch("{id:int}/avaliar")]
    [Authorize(Roles = Perfis.Admin)]
    public async Task<ActionResult<VerificacaoResponse>> Avaliar(int id, [FromBody] VerificacaoAvaliacaoRequest request, CancellationToken ct)
    {
        var verificacao = await _context.Verificacoes.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (verificacao is null)
            throw BusinessException.NaoEncontrado("VERIFICACAO_NAO_ENCONTRADA", "Verificação não encontrada.");

        verificacao.Status = request.Aprovado ? StatusVerificacao.Aprovado : StatusVerificacao.Rejeitado;
        verificacao.Observacao = request.Observacao ?? verificacao.Observacao;

        if (request.Aprovado)
        {
            var profissional = await _context.Profissionais.FirstOrDefaultAsync(p => p.Id == verificacao.ProfissionalId, ct);
            if (profissional is not null)
            {
                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == profissional.UsuarioId, ct);
                if (usuario is not null)
                {
                    usuario.IdentidadeVerificada = true;
                }
            }
        }

        await _context.SaveChangesAsync(ct);

        return Ok(new VerificacaoResponse
        {
            Id = verificacao.Id,
            ProfissionalId = verificacao.ProfissionalId,
            TipoDocumento = verificacao.TipoDocumento,
            UrlDocumento = verificacao.UrlDocumento,
            Status = verificacao.Status.ToString(),
            CriadoEm = verificacao.CriadoEm,
            Observacao = verificacao.Observacao
        });
    }
}
