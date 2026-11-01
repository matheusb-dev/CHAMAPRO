using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Chamapro.Api.DTOs.Responses;
using Microsoft.EntityFrameworkCore.Storage;

namespace Chamapro.Api.Repositories;

public interface IPedidoRepository
{
    // Transação e trava de linha (o Service abre a transação; todos os repositórios compartilham o mesmo AppDbContext)
    Task<IDbContextTransaction> IniciarTransacaoAsync();
    /// <summary>SELECT ... FOR UPDATE no pedido. Serializa operações concorrentes sobre o mesmo pedido. Só dentro de transação.</summary>
    Task BloquearAsync(int pedidoId);
    Task SalvarAsync();

    // Consultas de apoio
    Task<int?> ObterProfissionalIdPorUsuarioAsync(int usuarioId);
    Task<bool> CategoriaExisteAsync(int categoriaId);
    Task<int> ContarPendentesDoClienteAsync(int clienteId);
    Task<bool> ClienteEhDonoAsync(int pedidoId, int clienteId);
    void AdicionarHistorico(int pedidoId, int? atorId, StatusPedido? anterior, StatusPedido novo, string detalhe);
    Task<IReadOnlyList<PedidoHistoricoResponse>?> ListarHistoricoAsync(int pedidoId, int? clienteId, int? profissionalId);
    /// <summary>Dos ids informados, devolve os que atendem a categoria e a cidade (RN04).</summary>
    Task<HashSet<int>> FiltrarProfissionaisQueAtendemAsync(IReadOnlyList<int> profissionalIds, int categoriaId, string cidade, string? bairro,
        TipoAtendimento tipoAtendimento, DateTime? dataDesejada);
    Task<List<int>> SelecionarProfissionaisCompativeisAsync(int categoriaId, string cidade, string? bairro,
        TipoAtendimento tipoAtendimento, DateTime? dataDesejada, int limite = 5);
    Task<List<int>> ListarUsuarioIdsProfissionaisAsync(IReadOnlyList<int> profissionalIds);

    // Pedidos
    Task AdicionarAsync(Pedido pedido);
    /// <summary>Rastreado, com destinatários, orçamentos e agendamento. Usar depois de BloquearAsync.</summary>
    Task<Pedido?> ObterParaAtualizacaoAsync(int id);
    /// <summary>Somente leitura, com tudo o que o detalhe precisa.</summary>
    Task<Pedido?> ObterDetalheAsync(int id);
    Task<(IReadOnlyList<Pedido> Itens, int Total)> ListarAsync(
        int? clienteId, int? profissionalId, StatusPedido? status, int pagina, int tamanho);
}
