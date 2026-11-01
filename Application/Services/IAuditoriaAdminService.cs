namespace Chamapro.Api.Services;

public interface IAuditoriaAdminService
{
    void Registrar(int adminId, string acao, string entidade, int entidadeId,
        string? estadoAnterior = null, string? estadoNovo = null);
}
