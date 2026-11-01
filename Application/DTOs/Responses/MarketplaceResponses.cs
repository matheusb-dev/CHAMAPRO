namespace Chamapro.Api.DTOs.Responses;

public sealed record FavoritoResponse(int ProfissionalId, string Nome, string? FotoUrl, decimal NotaMedia, DateTime CriadoEm);
public sealed record NotificacaoResponse(long Id, string Tipo, string Titulo, string Mensagem, string Dados, DateTime CriadaEm, DateTime? LidaEm);
