using System.Text.Json.Serialization;

namespace Chamapro.Api.Models.Enums;

/// <summary>Espelha o tipo status_agendamento do banco.</summary>
[JsonConverter(typeof(SnakeUpperEnumConverter<StatusAgendamento>))]
public enum StatusAgendamento
{
    Agendado,
    Confirmado,
    Concluido,
    Cancelado
}
