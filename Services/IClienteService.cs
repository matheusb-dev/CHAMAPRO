using Chamapro.Api.DTOs.Requests;

namespace Chamapro.Api.Services;

public interface IClienteService
{
    Task AtualizarPerfilAsync(AtualizarClienteRequest request, CancellationToken ct = default);
}