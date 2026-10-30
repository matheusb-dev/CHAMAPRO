using Chamapro.Api.Models.Enums;
using static Chamapro.Api.Models.Enums.StatusPedido;
using static Chamapro.Api.Services.AtorTransicao;

namespace Chamapro.Api.Services;

public enum AtorTransicao
{
    Cliente,
    Profissional,
    Sistema
}

/// <summary>
/// Tabela do ciclo de vida do pedido (seção 3.5 da Definição da API). Uso compartilhado com a P4 (agendamento).
/// CONCLUIDO e CANCELADO são estados finais: não aparecem como origem.
/// </summary>
public static class PedidoTransicoes
{
    private static readonly Dictionary<(StatusPedido De, StatusPedido Para), AtorTransicao[]> Regras = new()
    {
        [(Pendente, Aceito)]        = [Cliente],                 // POST /orcamentos/{id}/aceitar
        [(Pendente, Cancelado)]     = [Cliente, Sistema],        // cancelar; todos recusaram; 7 dias sem orçamento aceito
        [(Aceito, Agendado)]        = [Profissional],            // POST /agendamentos/{id}/confirmar
        [(Aceito, Cancelado)]       = [Cliente, Profissional],   // POST /pedidos/{id}/cancelar
        [(Agendado, EmAndamento)]   = [Profissional],            // PATCH /pedidos/{id}/status
        [(Agendado, Cancelado)]     = [Cliente, Profissional],   // cancelar (24 h ou menos = tardio)
        [(EmAndamento, Concluido)]  = [Profissional],            // PATCH /pedidos/{id}/status
    };

    public static bool Permitida(StatusPedido de, StatusPedido para, AtorTransicao ator)
        => Regras.TryGetValue((de, para), out var atores) && atores.Contains(ator);

    /// <summary>Lança 422 se a transição não existir na tabela. Os endpoints de pedido usam PEDIDO_STATUS_INVALIDO; o PATCH de status usa o padrão TRANSICAO_INVALIDA.</summary>
    public static void Garantir(StatusPedido de, StatusPedido para, AtorTransicao ator, string codigoErro = "TRANSICAO_INVALIDA")
    {
        if (!Permitida(de, para, ator))
            throw PedidoErros.Regra(codigoErro,
                $"Não é possível mudar o pedido de {EnumTexto.Rotulo(de)} para {EnumTexto.Rotulo(para)}.");
    }
}
