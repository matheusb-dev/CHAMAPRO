namespace Chamapro.Api.DTOs.Responses;

public sealed record AvaliacaoPendenteResponse(int PedidoId, string Titulo, int ProfissionalId, string ProfissionalNome, DateTime ConcluidoEm);
public sealed record AvaliacaoCriadaResponse(int Id, int PedidoId, int ProfissionalId, int Nota, string? Comentario, DateTime CriadoEm);
