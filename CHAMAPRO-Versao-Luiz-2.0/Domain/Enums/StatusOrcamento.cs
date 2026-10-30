using System.Text.Json.Serialization;

namespace Chamapro.Api.Models.Enums;

/// <summary>Espelha o tipo status_orcamento do banco (depois da mudança 001). "Pendente" é legado do script v1 e não é usado.</summary>
[JsonConverter(typeof(SnakeUpperEnumConverter<StatusOrcamento>))]
public enum StatusOrcamento
{
    Pendente,
    Enviado,
    Aceito,
    Recusado,
    NaoEscolhido
}
