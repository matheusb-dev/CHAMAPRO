using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;

namespace Chamapro.Api.Services;

public interface IOrcamentoService
{
    Task<OrcamentoResponse> EnviarAsync(int pedidoId, OrcamentoCriarRequest request);
    Task<IReadOnlyList<OrcamentoComProfissionalResponse>> ListarDoPedidoAsync(int pedidoId);
    Task<PedidoDetalheResponse> AceitarAsync(int orcamentoId);
    Task RecusarAsync(int orcamentoId);
}
