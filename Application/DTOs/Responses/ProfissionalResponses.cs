namespace Chamapro.Api.DTOs.Responses;

public record ProfissionalResumoResponse(
    int Id,
    string Nome,
    string? FotoUrl,
    IReadOnlyList<string> Especialidades,
    decimal NotaMedia,
    int TotalAvaliacoes,
    bool Verificado,
    decimal? FaixaPrecoMin,
    decimal? FaixaPrecoMax);

public record EspecialidadeResponse(string Categoria, decimal? FaixaPrecoMin, decimal? FaixaPrecoMax);

public record PerfilPublicoResponse(
    int Id,
    string Nome,
    string? FotoUrl,
    string? Descricao,
    IReadOnlyList<EspecialidadeResponse> Especialidades,
    IReadOnlyList<string> AreasAtendimento,
    decimal NotaMedia,
    int TotalAvaliacoes,
    bool Verificado,
    IReadOnlyList<string> FotosTrabalhos,
    DateTime MembroDesde);

public record AvaliacaoPublicaResponse(int Nota, string? Comentario, string NomeCliente, DateTime Data);