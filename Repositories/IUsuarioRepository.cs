using Microsoft.EntityFrameworkCore;
using Chamapro.Api.Data;
using Chamapro.Api.Models.Entities;

namespace Chamapro.Api.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default);
    Task<Usuario?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<Usuario> CriarAsync(Usuario usuario, CancellationToken ct = default);
}

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _context;

    public UsuarioRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default)
        => await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<Usuario?> ObterPorIdAsync(int id, CancellationToken ct = default)
        => await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<Usuario> CriarAsync(Usuario usuario, CancellationToken ct = default)
    {
        await _context.Usuarios.AddAsync(usuario, ct);
        await _context.SaveChangesAsync(ct);
        return usuario;
    }
}
