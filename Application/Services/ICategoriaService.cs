using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;

namespace Chamapro.Api.Services;

public interface ICategoriaService
{
    Task<IReadOnlyList<CategoriaResponse>> ListarAsync(CancellationToken ct = default);
    Task<CategoriaResponse> CriarAsync(CategoriaRequest request, CancellationToken ct = default);
    Task<CategoriaResponse> AtualizarAsync(int id, CategoriaRequest request, CancellationToken ct = default);

    Task RemoverAsync(int id, CancellationToken ct = default);
}
