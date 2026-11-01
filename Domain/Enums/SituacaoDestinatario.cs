using System.Text.Json.Serialization;

namespace Chamapro.Api.Models.Enums;

/// <summary>Situação de cada profissional que recebeu o pedido (tipo situacao_destinatario, criado na mudança 002).</summary>
[JsonConverter(typeof(SnakeUpperEnumConverter<SituacaoDestinatario>))]
public enum SituacaoDestinatario
{
    Pendente,
    Aceito,
    Recusado,
    NaoEscolhido
}
