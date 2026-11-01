using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Microsoft.EntityFrameworkCore.Storage;

namespace Chamapro.Api.Repositories;

public interface IPedidoRepository
{
    Task<IDbContextTransaction> IniciarTransacaoAsync();
    Task BloquearAsync(int pedidoId);
    Task SalvarAsync();

    Task<int?> ObterProfissionalIdPorUsuarioAsync(int usuarioId);
    Task<Profissional?> ObterProfissionalPorIdAsync(int profissionalId);
    Task<bool> CategoriaExisteAsync(int categoriaId);
    Task<int> ContarPendentesDoClienteAsync(int clienteId);
    Task<bool> ClienteEhDonoAsync(int pedidoId, int clienteId);
    Task<HashSet<int>> FiltrarProfissionaisQueAtendemAsync(IReadOnlyList<int> profissionalIds, int categoriaId, string cidade);

    Task AdicionarAsync(Pedido pedido);
    Task<Pedido?> ObterParaAtualizacaoAsync(int id);
    Task<Pedido?> ObterDetalheAsync(int id);
    Task<(IReadOnlyList<Pedido> Itens, int Total)> ListarAsync(
        int? clienteId, int? profissionalId, StatusPedido? status, int pagina, int tamanho);
}