using Chamapro.Api.DTOs.Requests;

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
    short? ExperienciaAnos,
    decimal? DistanciaMaxKm,
    bool AtendeUrgencia,
    bool AtendeDomicilio,
    bool AtendeEstabelecimento,
    decimal NotaMedia,
    int TotalAvaliacoes,
    bool Verificado,
    IReadOnlyList<string> FotosTrabalhos,
    DateTime MembroDesde);

public record AvaliacaoPublicaResponse(int Id, int Nota, string? Comentario, string NomeCliente, DateTime Data);

public record ProfissionalMeuPerfilResponse(
    int Id, string Nome, string? Telefone, string? Descricao, string? FotoUrl, IReadOnlyList<string> FotosTrabalhos,
    IReadOnlyList<ServicoProfissionalRequest> Servicos, IReadOnlyList<AreaAtendimentoRequest> Areas,
    IReadOnlyList<DisponibilidadeProfissionalRequest> Disponibilidade,
    short? ExperienciaAnos, decimal? DistanciaMaxKm, bool AtendeUrgencia, bool AtendeDomicilio,
    bool AtendeEstabelecimento, bool Verificado, string StatusVerificacao);
