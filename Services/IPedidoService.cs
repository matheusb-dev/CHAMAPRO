using Chamapro.Api.Common;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;

namespace Chamapro.Api.Services;

public interface IPedidoService
{
    Task<PedidoCriadoResponse> CriarAsync(PedidoCriarRequest request);
    Task<PagedResponse<PedidoResumoResponse>> ListarAsync(PedidoListarQuery query);
    Task<PedidoDetalheResponse> ObterAsync(int id);
    Task AceitarAsync(int id);
    Task RecusarAsync(int id, PedidoRecusarRequest? request);
    Task CancelarAsync(int id, PedidoCancelarRequest request);
    Task<PedidoDetalheResponse> AtualizarStatusAsync(int id, PedidoStatusRequest request);
}
