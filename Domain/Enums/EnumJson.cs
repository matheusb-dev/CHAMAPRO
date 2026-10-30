using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chamapro.Api.Models.Enums;

/// <summary>Serializa enums como texto em maiúsculas com underscore (EmAndamento -> "EM_ANDAMENTO").</summary>
public sealed class SnakeUpperEnumConverter<T> : JsonStringEnumConverter<T> where T : struct, Enum
{
    public SnakeUpperEnumConverter() : base(JsonNamingPolicy.SnakeCaseUpper) { }
}

public static class EnumTexto
{
    /// <summary>Aceita "EM_ANDAMENTO", "em_andamento" ou "EmAndamento".</summary>
    public static bool TryParse<T>(string? texto, out T valor) where T : struct, Enum
    {
        valor = default;
        if (string.IsNullOrWhiteSpace(texto)) return false;

        var alvo = Normalizar(texto);
        foreach (var nome in Enum.GetNames<T>())
        {
            if (Normalizar(nome) == alvo)
            {
                valor = Enum.Parse<T>(nome);
                return true;
            }
        }
        return false;
    }

    /// <summary>Texto como viaja no JSON (ex.: "EM_ANDAMENTO").</summary>
    public static string Rotulo<T>(T valor) where T : struct, Enum
        => JsonNamingPolicy.SnakeCaseUpper.ConvertName(valor.ToString());

    private static string Normalizar(string s) => s.Replace("_", "").Trim().ToUpperInvariant();
}
