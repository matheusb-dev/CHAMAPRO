using Chamapro.Api.Common;
using Chamapro.Api.Data;
using Chamapro.Api.Models.Enums;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Controllers;

[Route(ApiRotas.Prefixo + "/whatsapp")]
public class WhatsAppController : ApiControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public WhatsAppController(AppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet("contato/me")]
    [Authorize]
    public async Task<ActionResult<WhatsAppContatoResponse>> GetMeuContato(CancellationToken ct)
    {
        var usuario = await _context.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == _currentUser.Id && x.Ativo, ct);
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

    [HttpPut("contato/me")]
    [Authorize]
    public async Task<ActionResult<WhatsAppContatoResponse>> AtualizarMeuContato(
        [FromBody] WhatsAppConfigRequest request, CancellationToken ct)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == _currentUser.Id && x.Ativo, ct);
        if (usuario is null)
            throw BusinessException.NaoEncontrado("USUARIO_NAO_ENCONTRADO", "Usuário não encontrado.");

        usuario.Telefone = request.Telefone.Trim();
        usuario.WhatsAppAtivo = request.WhatsAppAtivo;
        usuario.MensagemInicialWhatsApp = string.IsNullOrWhiteSpace(request.MensagemInicialWhatsApp)
            ? null : request.MensagemInicialWhatsApp.Trim();
        await _context.SaveChangesAsync(ct);

        return Ok(new WhatsAppContatoResponse
        {
            UsuarioId = usuario.Id,
            Telefone = usuario.Telefone,
            WhatsAppAtivo = usuario.WhatsAppAtivo,
            MensagemInicialWhatsApp = usuario.MensagemInicialWhatsApp
        });
    }

    [HttpGet("contato/{usuarioId:int}")]
    [Authorize]
    public async Task<ActionResult<WhatsAppContatoResponse>> GetContato(int usuarioId, CancellationToken ct)
    {
        var usuario = await _context.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == usuarioId && x.Ativo && x.WhatsAppAtivo && x.Telefone != null, ct);

        if (usuario is null || !await PodeContatarAsync(usuarioId, ct))
            throw BusinessException.NaoEncontrado("CONTATO_NAO_DISPONIVEL", "Contato não disponível para este pedido.");

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
        if (usuarioId != _currentUser.Id)
            throw BusinessException.Proibido("PERFIL_FORA_DO_ESCOPO", "Só é possível alterar o próprio contato.");

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

    [HttpGet("pedidos/{pedidoId:int}/contato")]
    [Authorize]
    public async Task<ActionResult<WhatsAppContatoResponse>> GetContatoDoPedido(int pedidoId, CancellationToken ct)
    {
        var pedido = await _context.Pedidos.AsNoTracking()
            .Include(p => p.Profissional).ThenInclude(p => p!.Usuario)
            .Include(p => p.Cliente)
            .FirstOrDefaultAsync(p => p.Id == pedidoId, ct);
        if (pedido is null || pedido.Profissional is null
            || pedido.Status is StatusPedido.Pendente or StatusPedido.Orcado or StatusPedido.Cancelado)
            throw BusinessException.NaoEncontrado("CONTATO_NAO_DISPONIVEL", "Contato não disponível para este pedido.");

        Usuario? destino = null;
        if (_currentUser.EhCliente && pedido.ClienteId == _currentUser.Id)
            destino = pedido.Profissional.Usuario;
        else if (_currentUser.EhProfissional && pedido.Profissional.UsuarioId == _currentUser.Id)
            destino = pedido.Cliente;

        if (destino is null || !destino.Ativo || !destino.WhatsAppAtivo || string.IsNullOrWhiteSpace(destino.Telefone))
            throw BusinessException.NaoEncontrado("CONTATO_NAO_DISPONIVEL", "Contato não disponível para este pedido.");

        return Ok(new WhatsAppContatoResponse
        {
            UsuarioId = destino.Id,
            Telefone = destino.Telefone,
            WhatsAppAtivo = true,
            MensagemInicialWhatsApp = destino.MensagemInicialWhatsApp
        });
    }

    private async Task<bool> PodeContatarAsync(int usuarioAlvoId, CancellationToken ct)
    {
        if (_currentUser.EhCliente)
            return await _context.Pedidos.AsNoTracking().AnyAsync(p =>
                p.ClienteId == _currentUser.Id
                && p.ProfissionalId != null
                && p.Profissional!.UsuarioId == usuarioAlvoId
                && p.Status != StatusPedido.Pendente
                && p.Status != StatusPedido.Orcado
                && p.Status != StatusPedido.Cancelado, ct);

        if (_currentUser.EhProfissional)
            return await _context.Pedidos.AsNoTracking().AnyAsync(p =>
                p.ClienteId == usuarioAlvoId
                && p.ProfissionalId != null
                && p.Profissional!.UsuarioId == _currentUser.Id
                && p.Status != StatusPedido.Pendente
                && p.Status != StatusPedido.Orcado
                && p.Status != StatusPedido.Cancelado, ct);

        return false;
    }
}
