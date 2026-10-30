using System.Text.Json.Serialization;

namespace Chamapro.Api.Models.Enums;

/// <summary>Espelha o tipo tipo_usuario do banco.</summary>
[JsonConverter(typeof(SnakeUpperEnumConverter<TipoUsuario>))]
public enum TipoUsuario
{
    Cliente,
    Profissional,
    Admin
}
