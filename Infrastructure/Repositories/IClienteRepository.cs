using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;

namespace Chamapro.Api.Repositories;

public interface IClienteRepository
{
    Task AtualizarAsync(int usuarioId, AtualizarClienteRequest request, CancellationToken ct = default);
    Task<ClientePerfilResponse?> ObterPerfilAsync(int usuarioId, CancellationToken ct = default);
}
