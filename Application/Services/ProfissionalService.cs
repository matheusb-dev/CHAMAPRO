using Chamapro.Api.Common;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Repositories;

namespace Chamapro.Api.Services;

public class ProfissionalService : IProfissionalService
{
    private readonly IProfissionalRepository _repositorio;
    private readonly ICategoriaRepository _categorias;
    private readonly ICurrentUser _usuario;

    public ProfissionalService(IProfissionalRepository repositorio, ICategoriaRepository categorias, ICurrentUser usuario)
    {
        _repositorio = repositorio;
        _categorias = categorias;
        _usuario = usuario;
    }

    public Task<RespostaPaginada<ProfissionalResumoResponse>> BuscarAsync(BuscaProfissionaisQuery filtro, CancellationToken ct = default)
        => _repositorio.BuscarAsync(filtro, ct);

    public async Task<PerfilPublicoResponse> ObterPerfilAsync(int id, CancellationToken ct = default)
    {
        return await _repositorio.ObterPerfilPublicoAsync(id, ct)
               ?? throw ProfissionalNaoEncontrado();
    }

    public async Task<ProfissionalMeuPerfilResponse> ObterMeuPerfilAsync(CancellationToken ct = default)
        => await _repositorio.ObterMeuPerfilAsync(_usuario.Id, ct)
           ?? throw PerfilNaoEncontrado();

    public async Task<RespostaPaginada<AvaliacaoPublicaResponse>> ListarAvaliacoesAsync(
        int id, PaginacaoQuery paginacao, CancellationToken ct = default)
    {
        return await _repositorio.ListarAvaliacoesAsync(id, paginacao, ct)
               ?? throw ProfissionalNaoEncontrado();
    }

    public async Task AtualizarPerfilAsync(AtualizarProfissionalRequest request, CancellationToken ct = default)
    {
        var achou = await _repositorio.AtualizarPerfilAsync(_usuario.Id, request, ct);
        if (!achou) throw PerfilNaoEncontrado();
    }

    public async Task DefinirServicosAsync(IReadOnlyList<ServicoProfissionalRequest> servicos, CancellationToken ct = default)
    {
        if (servicos is null || servicos.Count == 0)
            throw BusinessException.Invalido("SERVICOS_OBRIGATORIOS", "Informe pelo menos um serviço.");

        if (servicos.Select(s => s.CategoriaId).Distinct().Count() != servicos.Count)
            throw BusinessException.Invalido("CATEGORIA_REPETIDA", "Cada categoria só pode aparecer uma vez.");

        foreach (var servico in servicos)
        {
            var categoria = await _categorias.ObterPorIdAsync(servico.CategoriaId, ct);
            if (categoria is null || !categoria.Ativa)
                throw BusinessException.Regra("CATEGORIA_INVALIDA",
                    $"A categoria {servico.CategoriaId} não existe ou está inativa.");
        }

        var achou = await _repositorio.DefinirServicosAsync(_usuario.Id, servicos, ct);
        if (!achou) throw PerfilNaoEncontrado();
    }

    public async Task DefinirAreasAsync(IReadOnlyList<AreaAtendimentoRequest> areas, CancellationToken ct = default)
    {
        if (areas is null || areas.Count == 0)
            throw BusinessException.Invalido("AREAS_OBRIGATORIAS", "Informe pelo menos uma cidade atendida.");

        var achou = await _repositorio.DefinirAreasAsync(_usuario.Id, areas, ct);
        if (!achou) throw PerfilNaoEncontrado();
    }

    public async Task DefinirDisponibilidadeAsync(
        IReadOnlyList<DisponibilidadeProfissionalRequest> disponibilidade, CancellationToken ct = default)
    {
        if (disponibilidade is null || disponibilidade.Count > 42)
            throw BusinessException.Invalido("DISPONIBILIDADE_INVALIDA", "Informe até 42 faixas de disponibilidade.");
        if (disponibilidade.GroupBy(x => x.DiaSemana).Any(g =>
                g.OrderBy(x => x.HoraInicio).Zip(g.OrderBy(x => x.HoraInicio).Skip(1), (a, b) => a.HoraFim > b.HoraInicio).Any(x => x)))
            throw BusinessException.Invalido("HORARIO_SOBREPOSTO", "Os horários de um mesmo dia não podem se sobrepor.");

        var achou = await _repositorio.DefinirDisponibilidadeAsync(_usuario.Id, disponibilidade, ct);
        if (!achou) throw PerfilNaoEncontrado();
    }

    private static BusinessException ProfissionalNaoEncontrado()
        => BusinessException.NaoEncontrado("PROFISSIONAL_NAO_ENCONTRADO", "Profissional não encontrado.");

    private static BusinessException PerfilNaoEncontrado()
        => BusinessException.NaoEncontrado("PERFIL_NAO_ENCONTRADO", "Perfil profissional não encontrado.");
}
