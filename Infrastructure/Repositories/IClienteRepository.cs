using Chamapro.Api.DTOs.Requests;

namespace Chamapro.Api.Repositories;

public interface IClienteRepository
{
    Task AtualizarAsync(int usuarioId, AtualizarClienteRequest request, CancellationToken ct = default);
}