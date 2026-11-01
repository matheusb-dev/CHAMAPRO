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
    private readonly ICurrentUser _currentUser;

    public AdminController(AppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet("usuarios")]
    public async Task<ActionResult<List<UsuarioResumoResponse>>> ListarUsuarios([FromQuery] AdminUsuariosQuery query, CancellationToken ct)
    {
        IQueryable<Usuario> consulta = _context.Usuarios.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Busca))
        {
            var busca = query.Busca.Trim().ToLower();
            consulta = consulta.Where(u => u.Nome.ToLower().Contains(busca) || u.Email.ToLower().Contains(busca));
        }
        if (!string.IsNullOrWhiteSpace(query.Perfil)
            && Enum.TryParse<TipoUsuario>(query.Perfil, true, out var tipo))
            consulta = consulta.Where(u => u.Tipo == tipo);
        if (query.Ativo.HasValue)
            consulta = consulta.Where(u => u.Ativo == query.Ativo.Value);

        var usuarios = await consulta
            .OrderBy(u => u.Nome)
            .Take(200)
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

    [HttpGet("relatorios")]
    public async Task<ActionResult<AdminRelatorioResponse>> Relatorios(CancellationToken ct)
    {
        var usuarios = await _context.Usuarios.AsNoTracking().GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Clientes = g.Count(u => u.Tipo == TipoUsuario.Cliente),
                Profissionais = g.Count(u => u.Tipo == TipoUsuario.Profissional),
                Verificados = g.Count(u => u.Tipo == TipoUsuario.Profissional && u.IdentidadeVerificada)
            }).FirstOrDefaultAsync(ct);

        var pedidos = await _context.Pedidos.AsNoTracking().GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Pendentes = g.Count(p => p.Status == StatusPedido.Pendente),
                Concluidos = g.Count(p => p.Status == StatusPedido.Concluido),
                Cancelados = g.Count(p => p.Status == StatusPedido.Cancelado)
            }).FirstOrDefaultAsync(ct);

        var categorias = await _context.Pedidos.AsNoTracking()
            .GroupBy(p => p.Categoria.Nome)
            .Select(g => new AdminCategoriaUsoResponse(g.Key, g.Count()))
            .OrderByDescending(x => x.Chamados).ThenBy(x => x.Categoria).Take(5).ToListAsync(ct);

        var profissionais = await (from avaliacao in _context.Avaliacoes.AsNoTracking()
                                   join usuario in _context.Usuarios.AsNoTracking() on avaliacao.AvaliadoId equals usuario.Id
                                   where usuario.Tipo == TipoUsuario.Profissional
                                   group avaliacao by new { avaliacao.AvaliadoId, usuario.Nome } into grupo
                                   orderby grupo.Count() descending, grupo.Average(a => a.Nota) descending
                                   select new AdminProfissionalAvaliacaoResponse(
                                       grupo.Key.AvaliadoId, grupo.Key.Nome,
                                       Math.Round((decimal)grupo.Average(a => a.Nota), 1), grupo.Count()))
            .Take(5).ToListAsync(ct);

        return Ok(new AdminRelatorioResponse(
            usuarios?.Total ?? 0, usuarios?.Clientes ?? 0, usuarios?.Profissionais ?? 0, usuarios?.Verificados ?? 0,
            pedidos?.Total ?? 0, pedidos?.Pendentes ?? 0, pedidos?.Concluidos ?? 0, pedidos?.Cancelados ?? 0,
            categorias, profissionais));
    }

    [HttpGet("pedidos")]
    public async Task<ActionResult<List<AdminPedidoResponse>>> ListarPedidos(CancellationToken ct)
    {
        var pedidos = await _context.Pedidos
            .AsNoTracking()
            .OrderByDescending(p => p.CriadoEm)
            .Select(p => new AdminPedidoResponse(
                p.Id,
                p.Titulo,
                p.Categoria.Nome,
                p.ClienteId,
                p.Cliente.Nome,
                p.ProfissionalId,
                p.Profissional == null ? null : p.Profissional.Usuario.Nome,
                p.Cidade,
                p.Bairro,
                p.Status,
                p.CriadoEm))
            .ToListAsync(ct);

        return Ok(pedidos);
    }

    [HttpPatch("pedidos/{id:int}/status")]
    public async Task<ActionResult<AdminPedidoStatusResponse>> AlterarStatusPedido(int id, [FromBody] PedidoStatusRequest request, CancellationToken ct)
    {
        var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pedido is null)
            throw BusinessException.NaoEncontrado("PEDIDO_NAO_ENCONTRADO", "Pedido não encontrado.");

        if (request.Status is not { } novoStatus)
            throw BusinessException.Invalido("STATUS_INVALIDO", "Status do pedido inválido.");
        if (!Enum.IsDefined(novoStatus))
            throw BusinessException.Invalido("STATUS_INVALIDO", "Status do pedido inválido.");

        var statusAnterior = pedido.Status;
        pedido.Status = novoStatus;
        _context.HistoricosPedidos.Add(new HistoricoPedido
        {
            PedidoId = pedido.Id, AtorId = _currentUser.Id, StatusAnterior = statusAnterior.ToString(),
            StatusNovo = novoStatus.ToString(), Detalhe = "Status alterado pela administração.", CriadoEm = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(ct);
        return Ok(new AdminPedidoStatusResponse(pedido.Id, pedido.Status));
    }

    [HttpPatch("usuarios/{id:int}/bloquear")]
    public async Task<ActionResult> BloquearUsuario(int id, CancellationToken ct)
    {
        return await AtualizarAtivo(id, ativo: false, ct);
    }

    [HttpPatch("usuarios/{id:int}/status")]
    public async Task<ActionResult> AtualizarStatusUsuario(int id, [FromBody] AdminUsuarioStatusRequest request, CancellationToken ct)
        => await AtualizarAtivo(id, request.Ativo, ct);

    private async Task<ActionResult> AtualizarAtivo(int id, bool ativo, CancellationToken ct)
    {
        if (id == _currentUser.Id)
            throw BusinessException.Regra("ADMIN_PROPRIO_STATUS", "Não é possível bloquear ou desativar a própria conta administrativa.");

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (usuario is null)
            throw BusinessException.NaoEncontrado("USUARIO_NAO_ENCONTRADO", "Usuário não encontrado.");

        usuario.Ativo = ativo;
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }
}
