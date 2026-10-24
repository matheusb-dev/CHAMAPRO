using Chamapro.Api.Common;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;

namespace Chamapro.Api.Repositories;

public interface IProfissionalRepository
{
    Task<RespostaPaginada<ProfissionalResumoResponse>> BuscarAsync(BuscaProfissionaisQuery filtro, CancellationToken ct = default);

    Task<PerfilPublicoResponse?> ObterPerfilPublicoAsync(int id, CancellationToken ct = default);

    Task<RespostaPaginada<AvaliacaoPublicaResponse>?> ListarAvaliacoesAsync(int id, PaginacaoQuery paginacao, CancellationToken ct = default);

    Task<bool> AtualizarPerfilAsync(int usuarioId, AtualizarProfissionalRequest request, CancellationToken ct = default);

    Task<bool> DefinirServicosAsync(int usuarioId, IReadOnlyList<ServicoProfissionalRequest> servicos, CancellationToken ct = default);

    Task<bool> DefinirAreasAsync(int usuarioId, IReadOnlyList<AreaAtendimentoRequest> areas, CancellationToken ct = default);
}