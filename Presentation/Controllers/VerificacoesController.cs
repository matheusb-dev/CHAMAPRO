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

[Route(ApiRotas.Prefixo + "/verificacoes")]
public class VerificacoesController : ApiControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly INotificacaoService _notificacoes;
    private readonly IArquivoService _arquivos;

    public VerificacoesController(AppDbContext context, ICurrentUser currentUser, INotificacaoService notificacoes,
        IArquivoService arquivos)
    {
        _context = context;
        _currentUser = currentUser;
        _notificacoes = notificacoes;
        _arquivos = arquivos;
    }

    [HttpPost]
    [Authorize(Roles = Perfis.Profissional)]
    public async Task<ActionResult<VerificacaoResponse>> Solicitar([FromBody] VerificacaoDocumentosRequest request, CancellationToken ct)
    {
        if (!Uri.TryCreate(request.UrlDocumento, UriKind.Absolute, out var documentoUri)
            || !documentoUri.AbsolutePath.StartsWith(ArquivoService.RotaPrivada + "/", StringComparison.OrdinalIgnoreCase))
            throw BusinessException.Invalido("DOCUMENTO_PRIVADO_OBRIGATORIO", "Envie o documento pela área privada de arquivos.");
        var nomeArquivo = Path.GetFileName(documentoUri.AbsolutePath);
        var caminhoDocumento = _arquivos.ObterCaminhoDocumentoPrivado(nomeArquivo);
        var documentoDoUsuario = await _context.ArquivosPrivados.AsNoTracking()
            .AnyAsync(a => a.NomeArquivo == nomeArquivo && a.UsuarioId == _currentUser.Id, ct);
        if (!documentoDoUsuario || caminhoDocumento is null || !System.IO.File.Exists(caminhoDocumento))
            throw BusinessException.Invalido("DOCUMENTO_NAO_ENCONTRADO", "O arquivo privado não foi encontrado.");

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

    [HttpGet("me")]
    [Authorize(Roles = Perfis.Profissional)]
    public async Task<ActionResult<List<VerificacaoStatusResponse>>> MinhaSituacao(CancellationToken ct)
    {
        var profissionalId = await _context.Profissionais.AsNoTracking()
            .Where(p => p.UsuarioId == _currentUser.Id)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(ct);
        if (profissionalId is null)
            throw BusinessException.NaoEncontrado("PERFIL_NAO_ENCONTRADO", "Perfil profissional não encontrado.");

        var itens = await _context.Verificacoes.AsNoTracking()
            .Where(v => v.ProfissionalId == profissionalId.Value)
            .OrderByDescending(v => v.CriadoEm)
            .Select(v => new VerificacaoStatusResponse(v.Id, v.TipoDocumento, v.UrlDocumento, v.Status.ToString(), v.Observacao, v.CriadoEm))
            .ToListAsync(ct);
        return Ok(itens);
    }

    [HttpGet]
    [Authorize(Roles = Perfis.Admin)]
    public async Task<ActionResult<List<VerificacaoResponse>>> ListarPendentes(CancellationToken ct)
    {
        var itens = await _context.Verificacoes
            .Where(v => v.Status == StatusVerificacao.Pendente
                || v.Status == StatusVerificacao.EmAnalise
                || v.Status == StatusVerificacao.DocumentacaoPendente)
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

        var novoStatus = request.Status is not null
            ? Enum.TryParse<StatusVerificacao>(request.Status, true, out var statusInformado)
                ? statusInformado
                : throw BusinessException.Invalido("STATUS_INVALIDO", "Status de verificação inválido.")
            : request.Aprovado == true ? StatusVerificacao.Aprovado
            : request.Aprovado == false ? StatusVerificacao.Rejeitado
            : throw BusinessException.Invalido("STATUS_INVALIDO", "Informe uma decisão válida.");

        if (novoStatus == StatusVerificacao.Suspenso && verificacao.Status != StatusVerificacao.Aprovado)
            throw BusinessException.Regra("VERIFICACAO_NAO_APROVADA", "Somente um profissional aprovado pode ter a verificação suspensa.");

        verificacao.Status = novoStatus;
        verificacao.Observacao = request.Observacao ?? verificacao.Observacao;

        var profissional = await _context.Profissionais.FirstOrDefaultAsync(p => p.Id == verificacao.ProfissionalId, ct);
        if (profissional is not null)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == profissional.UsuarioId, ct);
            if (usuario is not null)
            {
                var outrasAprovadas = await _context.Verificacoes.AsNoTracking()
                    .AnyAsync(v => v.ProfissionalId == profissional.Id
                        && v.Id != verificacao.Id && v.Status == StatusVerificacao.Aprovado, ct);
                usuario.IdentidadeVerificada = novoStatus == StatusVerificacao.Aprovado
                    || (novoStatus != StatusVerificacao.Suspenso && outrasAprovadas);
            }
            _notificacoes.Adicionar(profissional.UsuarioId, "verificacao_atualizada", "Status da verificação atualizado",
                $"Sua verificação foi marcada como {novoStatus}.", new { verificacaoId = verificacao.Id, status = novoStatus.ToString() });
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
