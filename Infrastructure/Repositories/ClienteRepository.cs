using Chamapro.Api.DTOs.Requests;

namespace Chamapro.Api.Repositories;

public class ClienteRepository : IClienteRepository
{
    private static readonly object Trava = new();
    private static readonly Dictionary<int, AtualizarClienteRequest> Dados = new();

    public Task AtualizarAsync(int usuarioId, AtualizarClienteRequest request, CancellationToken ct = default)
    {
        lock (Trava)
        {
            Dados[usuarioId] = new AtualizarClienteRequest
            {
                Nome = request.Nome.Trim(),
                Telefone = request.Telefone,
                Cidade = request.Cidade.Trim(),
                FotoUrl = string.IsNullOrWhiteSpace(request.FotoUrl) ? null : request.FotoUrl.Trim()
            };
        }
        return Task.CompletedTask;
    }
}