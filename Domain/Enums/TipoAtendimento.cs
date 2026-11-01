using System.Text.Json.Serialization;

namespace Chamapro.Api.Models.Enums;

[JsonConverter(typeof(SnakeUpperEnumConverter<TipoAtendimento>))]
public enum TipoAtendimento
{
    Imediato,
    Agendado
}
