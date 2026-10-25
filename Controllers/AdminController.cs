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

[Route(ApiRotas.Prefixo + "/admin")]
[Authorize(Roles = Perfis.Admin)]
public class AdminController : ApiControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("usuarios")]
    public async Task<ActionResult<List<UsuarioResumoResponse>>> ListarUsuarios(CancellationToken ct)
    {
        var usuarios = await _context.Usuarios
            .OrderBy(u => u.Nome)
            .Select(u => new UsuarioResumoResponse
            {
                Id = u.Id,
                Nome = u.Nome,
                Email = u.Email,
                Perfil = u.Tipo.ToString(),
                Ativo = u.Ativo
            })
            .ToListAsync(ct);

        return Ok(usuarios);
    }

    [HttpGet("pedidos")]
    public async Task<ActionResult<List<Pedido>>> ListarPedidos(CancellationToken ct)
    {
        var pedidos = await _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Profissional)
            .Include(p => p.Categoria)
            .OrderByDescending(p => p.CriadoEm)
            .ToListAsync(ct);

        return Ok(pedidos);
    }

    [HttpPatch("pedidos/{id:int}/status")]
    public async Task<ActionResult<Pedido>> AlterarStatusPedido(int id, [FromBody] PedidoStatusRequest request, CancellationToken ct)
    {
        var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pedido is null)
            throw BusinessException.NaoEncontrado("PEDIDO_NAO_ENCONTRADO", "Pedido não encontrado.");

        if (!Enum.TryParse<StatusPedido>(request.Status, true, out var novoStatus))
            throw BusinessException.Invalido("STATUS_INVALIDO", "Status do pedido inválido.");

        pedido.Status = novoStatus;
        await _context.SaveChangesAsync(ct);
        return Ok(pedido);
    }

    [HttpPatch("usuarios/{id:int}/bloquear")]
    public async Task<ActionResult> BloquearUsuario(int id, CancellationToken ct)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (usuario is null)
            throw BusinessException.NaoEncontrado("USUARIO_NAO_ENCONTRADO", "Usuário não encontrado.");

        usuario.Ativo = false;
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }
}
