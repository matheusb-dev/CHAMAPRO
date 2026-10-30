using System.Text.Json.Serialization;

namespace Chamapro.Api.Models.Enums;

/// <summary>Espelha o tipo status_pedido do banco. "Orcado" existe no banco, mas o ciclo da API (seção 3.5) não o usa.</summary>
[JsonConverter(typeof(SnakeUpperEnumConverter<StatusPedido>))]
public enum StatusPedido
{
    Pendente,
    Orcado,
    Aceito,
    Agendado,
    EmAndamento,
    Concluido,
    Cancelado
}
