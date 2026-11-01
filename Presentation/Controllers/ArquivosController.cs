
using Chamapro.Api.Common;
using Chamapro.Api.Data;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Controllers;

[Authorize]
[Route(ApiRotas.Prefixo + "/arquivos")]
public class ArquivosController : ApiControllerBase
{
    private readonly IArquivoService _servico;
    private readonly AppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public ArquivosController(IArquivoService servico, AppDbContext context, ICurrentUser currentUser)
    {
        _servico = servico;
        _context = context;
        _currentUser = currentUser;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<ActionResult<ArquivoResponse>> Enviar(
        IFormFile arquivo,
        [FromForm] bool privado,
        CancellationToken ct)
    {
        if (privado && !_currentUser.EhProfissional && !_currentUser.EhAdmin)
            throw BusinessException.Proibido("UPLOAD_PRIVADO_RESTRITO", "Somente profissionais podem enviar documentos de verificação.");

        var caminho = privado
            ? await _servico.SalvarDocumentoPrivadoAsync(arquivo, ct)
            : await _servico.SalvarImagemAsync(arquivo, ct);

        if (privado)
        {
            _context.ArquivosPrivados.Add(new ArquivoPrivado
            {
                NomeArquivo = Path.GetFileName(caminho),
                UsuarioId = _currentUser.Id,
                CriadoEm = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(ct);
        }

        var url = $"{Request.Scheme}://{Request.Host}{caminho}";

        return Created(url, new ArquivoResponse(url));
    }

    [HttpGet("privados/{nomeArquivo}")]
    public async Task<IActionResult> ObterDocumentoPrivado(string nomeArquivo, CancellationToken ct)
    {
        var caminho = _servico.ObterCaminhoDocumentoPrivado(nomeArquivo);
        if (caminho is null || !System.IO.File.Exists(caminho))
            throw BusinessException.NaoEncontrado("DOCUMENTO_NAO_ENCONTRADO", "Documento não encontrado.");

        var arquivoRegistrado = await _context.ArquivosPrivados.AsNoTracking()
            .AnyAsync(a => a.NomeArquivo == nomeArquivo && (_currentUser.EhAdmin || a.UsuarioId == _currentUser.Id), ct);
        if (!arquivoRegistrado)
            throw BusinessException.NaoEncontrado("DOCUMENTO_NAO_ENCONTRADO", "Documento não encontrado.");

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Cache-Control"] = "private, no-store";
        var contentType = Path.GetExtension(nomeArquivo).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
        return PhysicalFile(caminho, contentType, Path.GetFileName(nomeArquivo), enableRangeProcessing: true);
    }
}
