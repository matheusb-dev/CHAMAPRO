namespace Chamapro.Api.DTOs.Responses;

public record DenunciaResponse(
    int Id,
    int PedidoId,
    int DenunciadoId,
    string Motivo,
    string Tipo,
    string? Descricao,
    DateTime CriadoEm,
    string Status);