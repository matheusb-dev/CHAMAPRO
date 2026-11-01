using System.Globalization;
using System.Text;
using Chamapro.Api.Common;
using Chamapro.Api.Data;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Repositories;

public class ProfissionalRepository : IProfissionalRepository
{
    private readonly AppDbContext _context;

    public ProfissionalRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RespostaPaginada<ProfissionalResumoResponse>> BuscarAsync(
        BuscaProfissionaisQuery filtro,
        CancellationToken ct = default)
    {
        var categoriaId = filtro.CategoriaId ?? 0;
        var cidade = Normalizar(filtro.Cidade ?? string.Empty);
        var bairro = string.IsNullOrWhiteSpace(filtro.Bairro) ? null : Normalizar(filtro.Bairro);

        var query = _context.Profissionais
            .AsNoTracking()
            .Include(p => p.Usuario)
            .Include(p => p.Categorias)
                .ThenInclude(pc => pc.Categoria)
            .Include(p => p.Areas)
            .Where(p => p.Usuario.Ativo)
            .Where(p => p.Categorias.Any(pc => pc.CategoriaId == categoriaId && pc.Categoria.Ativa))
            .Where(p => p.Areas.Any(a =>
                Normalizar(a.Cidade) == cidade &&
                (bairro == null || string.IsNullOrWhiteSpace(a.Bairro) || Normalizar(a.Bairro) == bairro)));

        if (filtro.Verificado == true)
        {
            query = query.Where(p => p.Usuario.IdentidadeVerificada);
        }

        var profissionais = await query
            .OrderBy(p => p.Usuario.Nome)
            .ToListAsync(ct);

        var itens = new List<ProfissionalResumoResponse>();

        foreach (var p in profissionais)
        {
            var notaMedia = await _context.Avaliacoes
                .AsNoTracking()
                .Where(a => a.AvaliadoId == p.Id)
                .AverageAsync(a => (decimal?)a.Nota, ct) ?? 0m;

            var totalAvaliacoes = await _context.Avaliacoes
                .AsNoTracking()
                .CountAsync(a => a.AvaliadoId == p.Id, ct);

            var servicos = p.Categorias
                .Select(pc => pc.Categoria.Nome)
                .ToList();

            var categoriaSelecionada = p.Categorias
                .FirstOrDefault(pc => pc.CategoriaId == categoriaId);

            var item = new ProfissionalResumoResponse(
                p.Id,
                p.Usuario.Nome,
                null,
                servicos,
                Math.Round(notaMedia, 1),
                totalAvaliacoes,
                p.Usuario.IdentidadeVerificada,
                categoriaSelecionada == null ? null : p.PrecoMin,
                categoriaSelecionada == null ? null : p.PrecoMax);

            itens.Add(item);
        }

        if (filtro.NotaMinima is not null)
        {
            itens = itens
                .Where(i => i.NotaMedia >= filtro.NotaMinima.Value)
                .ToList();
        }

        IEnumerable<ProfissionalResumoResponse> ordenados = filtro.Ordenar switch
        {
            "avaliacoes" => itens
                .OrderByDescending(i => i.TotalAvaliacoes)
                .ThenByDescending(i => i.NotaMedia)
                .ThenBy(i => i.Nome),
            "preco" => itens
                .OrderBy(i => i.FaixaPrecoMin ?? decimal.MaxValue)
                .ThenByDescending(i => i.NotaMedia)
                .ThenBy(i => i.Nome),
            _ => itens
                .OrderByDescending(i => i.NotaMedia)
                .ThenByDescending(i => i.TotalAvaliacoes)
                .ThenBy(i => i.Nome)
        };

        var pagina = ordenados
            .Skip((filtro.Pagina - 1) * filtro.Tamanho)
            .Take(filtro.Tamanho)
            .ToList();

        return new RespostaPaginada<ProfissionalResumoResponse>
        {
            Itens = pagina,
            Pagina = filtro.Pagina,
            Tamanho = filtro.Tamanho,
            Total = ordenados.Count()
        };
    }

    public async Task<PerfilPublicoResponse?> ObterPerfilPublicoAsync(int id, CancellationToken ct = default)
    {
        var profissional = await _context.Profissionais
            .AsNoTracking()
            .Include(p => p.Usuario)
            .Include(p => p.Categorias)
                .ThenInclude(pc => pc.Categoria)
            .Include(p => p.Areas)
            .FirstOrDefaultAsync(p => p.Id == id && p.Usuario.Ativo, ct);

        if (profissional is null)
            return null;

        var especialidades = profissional.Categorias
            .Select(pc => new EspecialidadeResponse(
                pc.Categoria.Nome,
                profissional.PrecoMin,
                profissional.PrecoMax))
            .ToList();

        var areas = profissional.Areas
            .Select(a => string.IsNullOrWhiteSpace(a.Bairro) ? a.Cidade : $"{a.Cidade} - {a.Bairro}")
            .ToList();

        var notaMedia = await _context.Avaliacoes
            .AsNoTracking()
            .Where(a => a.AvaliadoId == profissional.Id)
            .AverageAsync(a => (decimal?)a.Nota, ct) ?? 0m;

        var totalAvaliacoes = await _context.Avaliacoes
            .AsNoTracking()
            .CountAsync(a => a.AvaliadoId == profissional.Id, ct);

        return new PerfilPublicoResponse(
            profissional.Id,
            profissional.Usuario.Nome,
            null,
            profissional.DescricaoPerfil,
            especialidades,
            areas,
            Math.Round(notaMedia, 1),
            totalAvaliacoes,
            profissional.Usuario.IdentidadeVerificada,
            new List<string>(),
            profissional.Usuario.CriadoEm);
    }

    public async Task<RespostaPaginada<AvaliacaoPublicaResponse>?> ListarAvaliacoesAsync(
        int id,
        PaginacaoQuery paginacao,
        CancellationToken ct = default)
    {
        var profissional = await _context.Profissionais
            .AsNoTracking()
            .Include(p => p.Usuario)
            .FirstOrDefaultAsync(p => p.Id == id && p.Usuario.Ativo, ct);

        if (profissional is null)
            return null;

        var avaliacoes = await _context.Avaliacoes
            .AsNoTracking()
            .Where(a => a.AvaliadoId == id)
            .OrderByDescending(a => a.Id)
            .Select(a => new
            {
                a.Nota,
                a.Comentario,
                NomeCliente = _context.Usuarios
                    .Where(u => u.Id == a.AvaliadorId)
                    .Select(u => u.Nome)
                    .FirstOrDefault(),
                a.Id
            })
            .Skip((paginacao.Pagina - 1) * paginacao.Tamanho)
            .Take(paginacao.Tamanho)
            .ToListAsync(ct);

        var total = await _context.Avaliacoes
            .AsNoTracking()
            .CountAsync(a => a.AvaliadoId == id, ct);

        var itens = avaliacoes
            .Select(a => new AvaliacaoPublicaResponse(
                a.Nota,
                a.Comentario,
                a.NomeCliente ?? "Cliente",
                DateTime.UtcNow))
            .ToList();

        return new RespostaPaginada<AvaliacaoPublicaResponse>
        {
            Itens = itens,
            Pagina = paginacao.Pagina,
            Tamanho = paginacao.Tamanho,
            Total = total
        };
    }

    public async Task<bool> AtualizarPerfilAsync(
        int usuarioId,
        AtualizarProfissionalRequest request,
        CancellationToken ct = default)
    {
        var profissional = await _context.Profissionais
            .Include(p => p.Usuario)
            .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, ct);

        if (profissional is null)
            return false;

        profissional.DescricaoPerfil = request.Descricao?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(request.Nome))
            profissional.Usuario.Nome = request.Nome.Trim();

        if (!string.IsNullOrWhiteSpace(request.Telefone))
            profissional.Usuario.Telefone = request.Telefone.Trim();

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DefinirServicosAsync(
        int usuarioId,
        IReadOnlyList<ServicoProfissionalRequest> servicos,
        CancellationToken ct = default)
    {
        var profissional = await _context.Profissionais
            .Include(p => p.Categorias)
            .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, ct);

        if (profissional is null)
            return false;

        _context.ProfissionalCategorias.RemoveRange(profissional.Categorias);

        foreach (var servico in servicos)
        {
            _context.ProfissionalCategorias.Add(new ProfissionalCategoria
            {
                ProfissionalId = profissional.Id,
                CategoriaId = servico.CategoriaId
            });
        }

        var minPreco = servicos
            .Where(s => s.FaixaPrecoMin > 0m)
            .Select(s => s.FaixaPrecoMin)
            .DefaultIfEmpty(0m)
            .Min();

        var maxPreco = servicos
            .Where(s => s.FaixaPrecoMax > 0m)
            .Select(s => s.FaixaPrecoMax)
            .DefaultIfEmpty(0m)
            .Max();

        profissional.PrecoMin = minPreco == 0m ? null : minPreco;
        profissional.PrecoMax = maxPreco == 0m ? null : maxPreco;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DefinirAreasAsync(
        int usuarioId,
        IReadOnlyList<AreaAtendimentoRequest> areas,
        CancellationToken ct = default)
    {
        var profissional = await _context.Profissionais
            .Include(p => p.Areas)
            .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, ct);

        if (profissional is null)
            return false;

        _context.AreasAtendimento.RemoveRange(profissional.Areas);

        foreach (var area in areas)
        {
            if (string.IsNullOrWhiteSpace(area.Cidade))
                continue;

            _context.AreasAtendimento.Add(new AreaAtendimento
            {
                ProfissionalId = profissional.Id,
                Cidade = area.Cidade.Trim(),
                Bairro = string.IsNullOrWhiteSpace(area.Bairro) ? null : area.Bairro.Trim()
            });
        }

        await _context.SaveChangesAsync(ct);
        return true;
    }

    private static string Normalizar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        var normalized = texto.Trim()
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString()
            .Normalize(NormalizationForm.FormC);
    }
}