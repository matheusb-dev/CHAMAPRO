using Chamapro.Api.Common;
using Chamapro.Api.Data;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Controllers;

[Route(ApiRotas.Prefixo + "/whatsapp")]
public class WhatsAppController : ApiControllerBase
{
    private readonly AppDbContext _context;

    public WhatsAppController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("contato/{usuarioId:int}")]
    [Authorize]
    public async Task<ActionResult<WhatsAppContatoResponse>> GetContato(int usuarioId, CancellationToken ct)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(x => x.Id == usuarioId, ct);

        if (usuario is null)
            throw BusinessException.NaoEncontrado("USUARIO_NAO_ENCONTRADO", "Usuário não encontrado.");

        return Ok(new WhatsAppContatoResponse
        {
            UsuarioId = usuario.Id,
            Telefone = usuario.Telefone,
            WhatsAppAtivo = usuario.WhatsAppAtivo,
            MensagemInicialWhatsApp = usuario.MensagemInicialWhatsApp
        });
    }

    [HttpPost("contato/{usuarioId:int}/configurar")]
    [Authorize]
    public async Task<ActionResult<WhatsAppContatoResponse>> ConfigurarContato(
        int usuarioId,
        [FromBody] WhatsAppConfigRequest request,
        CancellationToken ct)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(x => x.Id == usuarioId, ct);

        if (usuario is null)
            throw BusinessException.NaoEncontrado("USUARIO_NAO_ENCONTRADO", "Usuário não encontrado.");

        if (string.IsNullOrWhiteSpace(request.Telefone))
            throw BusinessException.Invalido("TELEFONE_INVALIDO", "Informe um telefone válido.");

        usuario.Telefone = request.Telefone.Trim();
        usuario.WhatsAppAtivo = request.WhatsAppAtivo;
        usuario.MensagemInicialWhatsApp = request.MensagemInicialWhatsApp?.Trim();

        await _context.SaveChangesAsync(ct);

        return Ok(new WhatsAppContatoResponse
        {
            UsuarioId = usuario.Id,
            Telefone = usuario.Telefone,
            WhatsAppAtivo = usuario.WhatsAppAtivo,
            MensagemInicialWhatsApp = usuario.MensagemInicialWhatsApp
        });
    }
}