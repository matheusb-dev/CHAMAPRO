using Chamapro.Api.Common;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.Repositories;

namespace Chamapro.Api.Services;

public class ClienteService : IClienteService
{
    private readonly IClienteRepository _repositorio;
    private readonly ICurrentUser _usuario;

    public ClienteService(IClienteRepository repositorio, ICurrentUser usuario)
    {
        _repositorio = repositorio;
        _usuario = usuario;
    }

    public Task AtualizarPerfilAsync(AtualizarClienteRequest request, CancellationToken ct = default)
        => _repositorio.AtualizarAsync(_usuario.Id, request, ct);
}