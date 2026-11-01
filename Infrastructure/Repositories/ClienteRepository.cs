using Chamapro.Api.Common;
using Chamapro.Api.Data;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly AppDbContext _db;

    public ClienteRepository(AppDbContext db) => _db = db;

    public async Task<ClientePerfilResponse?> ObterPerfilAsync(int usuarioId, CancellationToken ct = default)
        => await _db.Usuarios.AsNoTracking()
            .Where(u => u.Id == usuarioId && u.Tipo == TipoUsuario.Cliente)
            .Select(u => new ClientePerfilResponse(u.Id, u.Nome, u.Email, u.Telefone, u.Cidade, u.Bairro,
                u.EnderecoPrincipal, u.PreferenciaContato, u.FotoUrl))
            .FirstOrDefaultAsync(ct);

    public async Task AtualizarAsync(int usuarioId, AtualizarClienteRequest request, CancellationToken ct = default)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId, ct)
            ?? throw BusinessException.NaoEncontrado("USUARIO_NAO_ENCONTRADO", "Usuário não encontrado.");

        if (usuario.Tipo != TipoUsuario.Cliente)
            throw BusinessException.Proibido("PERFIL_INVALIDO", "Este perfil não pertence a um cliente.");

        usuario.Nome = request.Nome.Trim();
        usuario.Telefone = request.Telefone.Trim();
        usuario.Cidade = request.Cidade.Trim();
        usuario.Bairro = string.IsNullOrWhiteSpace(request.Bairro) ? null : request.Bairro.Trim();
        usuario.EnderecoPrincipal = string.IsNullOrWhiteSpace(request.EnderecoPrincipal) ? null : request.EnderecoPrincipal.Trim();
        usuario.PreferenciaContato = string.IsNullOrWhiteSpace(request.PreferenciaContato) ? null : request.PreferenciaContato.Trim();
        usuario.FotoUrl = string.IsNullOrWhiteSpace(request.FotoUrl) ? null : request.FotoUrl.Trim();

        await _db.SaveChangesAsync(ct);
    }
}
