using Chamapro.Api.Common;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Repositories;

namespace Chamapro.Api.Services;

public class CategoriaService : ICategoriaService
{
    private readonly ICategoriaRepository _repositorio;

    public CategoriaService(ICategoriaRepository repositorio) => _repositorio = repositorio;

    public async Task<IReadOnlyList<CategoriaResponse>> ListarAsync(CancellationToken ct = default)
    {
        var categorias = await _repositorio.ListarAtivasAsync(ct);
        return categorias.Select(ParaResponse).ToList();
    }

    public async Task<CategoriaResponse> CriarAsync(CategoriaRequest request, CancellationToken ct = default)
    {
        var nome = request.Nome.Trim();

        if (await _repositorio.ExisteNomeAsync(nome, null, ct))
            throw BusinessException.Conflito("CATEGORIA_DUPLICADA", "Já existe uma categoria com este nome.");

        var categoria = await _repositorio.AdicionarAsync(new Categoria
        {
            Nome = nome,
            Icone = LimparIcone(request.Icone),
            Ativa = true
        }, ct);

        return ParaResponse(categoria);
    }

    public async Task<CategoriaResponse> AtualizarAsync(int id, CategoriaRequest request, CancellationToken ct = default)
    {
        var categoria = await ObterOuFalharAsync(id, ct);
        var nome = request.Nome.Trim();

        if (await _repositorio.ExisteNomeAsync(nome, id, ct))
            throw BusinessException.Conflito("CATEGORIA_DUPLICADA", "Já existe uma categoria com este nome.");

        categoria.Nome = nome;
        categoria.Icone = LimparIcone(request.Icone);
        await _repositorio.AtualizarAsync(categoria, ct);

        return ParaResponse(categoria);
    }

    public async Task RemoverAsync(int id, CancellationToken ct = default)
    {
        var categoria = await ObterOuFalharAsync(id, ct);

        if (await _repositorio.TemVinculosAsync(id, ct))
        {
            categoria.Ativa = false;
            await _repositorio.AtualizarAsync(categoria, ct);
            return;
        }

        await _repositorio.RemoverAsync(categoria, ct);
    }

    private async Task<Categoria> ObterOuFalharAsync(int id, CancellationToken ct)
    {
        return await _repositorio.ObterPorIdAsync(id, ct)
               ?? throw BusinessException.NaoEncontrado("CATEGORIA_NAO_ENCONTRADA", "Categoria não encontrada.");
    }

    private static string? LimparIcone(string? icone)
        => string.IsNullOrWhiteSpace(icone) ? null : icone.Trim();

    private static CategoriaResponse ParaResponse(Categoria c) => new(c.Id, c.Nome, c.Icone);
}
