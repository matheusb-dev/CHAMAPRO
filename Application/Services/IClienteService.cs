using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;

namespace Chamapro.Api.Services;

public interface IClienteService
{
    Task AtualizarPerfilAsync(AtualizarClienteRequest request, CancellationToken ct = default);
    Task<ClientePerfilResponse?> ObterPerfilAsync(CancellationToken ct = default);
}
