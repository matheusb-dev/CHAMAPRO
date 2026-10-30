using System.Text.Json;

namespace Chamapro.Api.Common;

/// <summary>
/// Faz os enums viajarem em MAIÚSCULAS no JSON ("PENDENTE", "EM_ANDAMENTO"), como o contrato da Parte 3.
/// Se o enum em C# se chamar EmAndamento, o JSON sai EM_ANDAMENTO.
/// </summary>
public class JsonUpperCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        var sb = new System.Text.StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]) && name[i - 1] != '_')
                sb.Append('_');
            sb.Append(char.ToUpperInvariant(c));
        }
        return sb.ToString();
    }
}
