using Chamapro.Api.Common;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;

namespace Chamapro.Api.Services;

public interface IProfissionalService
{
    Task<RespostaPaginada<ProfissionalResumoResponse>> BuscarAsync(BuscaProfissionaisQuery filtro, CancellationToken ct = default);
    Task<PerfilPublicoResponse> ObterPerfilAsync(int id, CancellationToken ct = default);
    Task<RespostaPaginada<AvaliacaoPublicaResponse>> ListarAvaliacoesAsync(int id, PaginacaoQuery paginacao, CancellationToken ct = default);

    Task AtualizarPerfilAsync(AtualizarProfissionalRequest request, CancellationToken ct = default);
    Task DefinirServicosAsync(IReadOnlyList<ServicoProfissionalRequest> servicos, CancellationToken ct = default);
    Task DefinirAreasAsync(IReadOnlyList<AreaAtendimentoRequest> areas, CancellationToken ct = default);
}