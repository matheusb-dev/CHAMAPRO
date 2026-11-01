using Chamapro.Api.Common;
using Chamapro.Api.Data;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Controllers;

[ApiController]
[Route(ApiRotas.Prefixo)]
[Authorize]
public sealed class MarketplaceController : ApiControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;

    public MarketplaceController(AppDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    [HttpGet("favoritos")]
    [Authorize(Roles = Perfis.Cliente)]
    public async Task<ActionResult<IReadOnlyList<FavoritoResponse>>> Favoritos(CancellationToken ct)
    {
        var itens = await _db.Favoritos.AsNoTracking()
            .Where(f => f.ClienteId == _user.Id && f.Profissional.Usuario.Ativo)
            .OrderByDescending(f => f.CriadoEm)
            .Select(f => new FavoritoResponse(
                f.ProfissionalId,
                f.Profissional.Usuario.Nome,
                f.Profissional.FotoUrl ?? f.Profissional.Usuario.FotoUrl,
                f.Profissional.MediaAvaliacao,
                f.CriadoEm))
            .ToListAsync(ct);
        return Ok(itens);
    }

    [HttpPost("favoritos/{profissionalId:int}")]
    [Authorize(Roles = Perfis.Cliente)]
    public async Task<IActionResult> Favoritar(int profissionalId, CancellationToken ct)
    {
        var existeProfissional = await _db.Profissionais.AsNoTracking()
            .AnyAsync(p => p.Id == profissionalId && p.Usuario.Ativo, ct);
        if (!existeProfissional)
            throw BusinessException.NaoEncontrado("PROFISSIONAL_NAO_ENCONTRADO", "Profissional não encontrado.");

        if (!await _db.Favoritos.AnyAsync(f => f.ClienteId == _user.Id && f.ProfissionalId == profissionalId, ct))
        {
            _db.Favoritos.Add(new Favorito { ClienteId = _user.Id, ProfissionalId = profissionalId });
            await _db.SaveChangesAsync(ct);
        }
        return NoContent();
    }

    [HttpDelete("favoritos/{profissionalId:int}")]
    [Authorize(Roles = Perfis.Cliente)]
    public async Task<IActionResult> Desfavoritar(int profissionalId, CancellationToken ct)
    {
        await _db.Favoritos.Where(f => f.ClienteId == _user.Id && f.ProfissionalId == profissionalId)
            .ExecuteDeleteAsync(ct);
        return NoContent();
    }

    [HttpGet("notificacoes")]
    public async Task<ActionResult<IReadOnlyList<NotificacaoResponse>>> Notificacoes(CancellationToken ct)
    {
        var itens = await _db.Notificacoes.AsNoTracking()
            .Where(n => n.UsuarioId == _user.Id)
            .OrderByDescending(n => n.CriadaEm).Take(100)
            .Select(n => new NotificacaoResponse(n.Id, n.Tipo, n.Titulo, n.Mensagem, n.Dados, n.CriadaEm, n.LidaEm))
            .ToListAsync(ct);
        return Ok(itens);
    }

    [HttpPatch("notificacoes/{id:long}/lida")]
    public async Task<IActionResult> MarcarNotificacaoLida(long id, CancellationToken ct)
    {
        var atualizada = await _db.Notificacoes
            .Where(n => n.Id == id && n.UsuarioId == _user.Id && n.LidaEm == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.LidaEm, DateTime.UtcNow), ct);
        if (atualizada == 0 && !await _db.Notificacoes.AnyAsync(n => n.Id == id && n.UsuarioId == _user.Id, ct))
            throw BusinessException.NaoEncontrado("NOTIFICACAO_NAO_ENCONTRADA", "Notificação não encontrada.");
        return NoContent();
    }

    [HttpPatch("notificacoes/lidas")]
    public async Task<IActionResult> MarcarTodasLidas(CancellationToken ct)
    {
        await _db.Notificacoes.Where(n => n.UsuarioId == _user.Id && n.LidaEm == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.LidaEm, DateTime.UtcNow), ct);
        return NoContent();
    }
}
