using Chamapro.Api.Data;
using Chamapro.Api.Models.Entities;

namespace Chamapro.Api.Services;

public sealed class AuditoriaAdminService : IAuditoriaAdminService
{
    private readonly AppDbContext _db;

    public AuditoriaAdminService(AppDbContext db) => _db = db;

    public void Registrar(int adminId, string acao, string entidade, int entidadeId,
        string? estadoAnterior = null, string? estadoNovo = null)
    {
        _db.AuditoriasAdmin.Add(new AuditoriaAdmin
        {
            AdminId = adminId,
            Acao = acao,
            Entidade = entidade,
            EntidadeId = entidadeId,
            EstadoAnterior = estadoAnterior,
            EstadoNovo = estadoNovo,
            CriadoEm = DateTime.UtcNow
        });
    }
}
