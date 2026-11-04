
using System.Globalization;
using System.Text;
using Chamapro.Api.Common;
using Chamapro.Api.Data;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Chamapro.Api.Models.Time;
using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Repositories;

public class ProfissionalRepository : IProfissionalRepository
{
    private readonly AppDbContext _db;

    public ProfissionalRepository(AppDbContext db) => _db = db;

    public async Task<RespostaPaginada<ProfissionalResumoResponse>> BuscarAsync(
        BuscaProfissionaisQuery filtro, CancellationToken ct = default)
    {
        var categoriaId = filtro.CategoriaId ?? 0;
        var cidade = (filtro.Cidade ?? string.Empty).Trim().ToLowerInvariant();
        var bairro = string.IsNullOrWhiteSpace(filtro.Bairro)
            ? null
            : filtro.Bairro.Trim().ToLowerInvariant();

        var consulta = _db.Profissionais.AsNoTracking()
            .Include(p => p.Usuario)
            .Include(p => p.Categorias).ThenInclude(pc => pc.Categoria)
            .Include(p => p.Areas)
            .AsSplitQuery()
            .Where(p => p.Usuario.Ativo
                && p.Categorias.Any(pc => pc.CategoriaId == categoriaId && pc.Categoria.Ativa)
                && p.Areas.Any(a => a.Cidade.ToLower() == cidade
                    && (bairro == null || a.Bairro == null || a.Bairro.ToLower() == bairro))
                && (filtro.Verificado != true || p.Usuario.IdentidadeVerificada));

        var profissionais = await consulta.ToListAsync(ct);

        var usuarioIds = profissionais.Select(p => p.UsuarioId).ToArray();

        var avaliacoes = await _db.Avaliacoes.AsNoTracking()
            .Where(a => usuarioIds.Contains(a.AvaliadoId))
            .GroupBy(a => a.AvaliadoId)
            .Select(g => new
            {
                UsuarioId = g.Key,
                Media = g.Average(a => a.Nota),
                Total = g.Count()
            })
            .ToDictionaryAsync(x => x.UsuarioId, ct);

        var itens = profissionais.Select(p =>
        {
            avaliacoes.TryGetValue(p.UsuarioId, out var avaliacao);

            return ParaResumo(
                p,
                categoriaId,
                avaliacao?.Media,
                avaliacao?.Total ?? 0);
        })
        .Where(x => filtro.NotaMinima is null ||
                    x.NotaMedia >= filtro.NotaMinima.Value)
        .ToList();

        IEnumerable<ProfissionalResumoResponse> ordenados = filtro.Ordenar switch
        {
            "avaliacoes" => itens.OrderByDescending(i => i.TotalAvaliacoes)
                .ThenByDescending(i => i.NotaMedia)
                .ThenBy(i => i.Nome),

            "preco" => itens.OrderBy(i => i.FaixaPrecoMin ?? decimal.MaxValue)
                .ThenByDescending(i => i.NotaMedia)
                .ThenBy(i => i.Nome),

            _ => itens.OrderByDescending(i => i.NotaMedia)
                .ThenByDescending(i => i.TotalAvaliacoes)
                .ThenBy(i => i.Nome)
        };

        var total = itens.Count;

        var pagina = ordenados
            .Skip((filtro.Pagina - 1) * filtro.Tamanho)
            .Take(filtro.Tamanho)
            .ToList();

        return new RespostaPaginada<ProfissionalResumoResponse>
        {
            Itens = pagina,
            Pagina = filtro.Pagina,
            Tamanho = filtro.Tamanho,
            Total = total
        };
    }

    public async Task<PerfilPublicoResponse?> ObterPerfilPublicoAsync(
        int id, CancellationToken ct = default)
    {
        var p = await _db.Profissionais.AsNoTracking()
            .Include(x => x.Usuario)
            .Include(x => x.Categorias).ThenInclude(pc => pc.Categoria)
            .Include(x => x.Areas)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id && x.Usuario.Ativo, ct);

        if (p is null) return null;

        var (media, total) = await ObterResumoAvaliacoesAsync(p.UsuarioId, ct);

        return new PerfilPublicoResponse(
            p.Id,
            p.Usuario.Nome,
            p.FotoUrl ?? p.Usuario.FotoUrl,
            p.DescricaoPerfil,
            p.Categorias.Where(pc => pc.Categoria.Ativa)
                .OrderBy(pc => pc.Categoria.Nome)
                .Select(pc => new EspecialidadeResponse(
                    pc.Categoria.Nome,
                    pc.FaixaPrecoMin,
                    pc.FaixaPrecoMax))
                .ToList(),
            p.Areas.Select(a => a.Bairro is null
                ? a.Cidade
                : $"{a.Cidade} - {a.Bairro}").ToList(),
            p.ExperienciaAnos,
            p.DistanciaMaxKm,
            p.AtendeUrgencia,
            p.AtendeDomicilio,
            p.AtendeEstabelecimento,
            media,
            total,
            p.Usuario.IdentidadeVerificada,
            p.FotosTrabalhos,
            p.Usuario.CriadoEm);
    }

    public async Task<ProfissionalMeuPerfilResponse?> ObterMeuPerfilAsync(
        int usuarioId, CancellationToken ct = default)
    {
        var p = await _db.Profissionais.AsNoTracking()
            .Include(x => x.Usuario)
            .Include(x => x.Categorias).ThenInclude(x => x.Categoria)
            .Include(x => x.Areas)
            .Include(x => x.Disponibilidade)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId && x.Usuario.Ativo, ct);

        if (p is null) return null;

        var cadastroCompleto = !string.IsNullOrWhiteSpace(p.DescricaoPerfil)
            && p.Categorias.Any(c => c.Categoria.Ativa)
            && p.Categorias.Where(c => c.Categoria.Ativa)
                .All(c => (c.FaixaPrecoMin ?? p.PrecoMin).HasValue
                    && (c.FaixaPrecoMax ?? p.PrecoMax).HasValue)
            && p.Areas.Count > 0
            && p.DistanciaMaxKm > 0
            && (p.AtendeDomicilio || p.AtendeEstabelecimento);

        var verificacao = await _db.Verificacoes.AsNoTracking()
            .Where(v => v.ProfissionalId == p.Id)
            .OrderByDescending(v => v.CriadoEm)
            .Select(v => (string?)v.Status.ToString())
            .FirstOrDefaultAsync(ct)
            ?? (cadastroCompleto ? "AguardandoVerificacao" : "CadastroIncompleto");

        return new ProfissionalMeuPerfilResponse(
            p.Id,
            p.Usuario.Nome,
            p.Usuario.Telefone,
            p.DescricaoPerfil,
            p.FotoUrl ?? p.Usuario.FotoUrl,
            p.FotosTrabalhos,
            p.Categorias.Select(c => new ServicoProfissionalRequest
            {
                CategoriaId = c.CategoriaId,
                FaixaPrecoMin = c.FaixaPrecoMin ?? p.PrecoMin,
                FaixaPrecoMax = c.FaixaPrecoMax ?? p.PrecoMax
            }).ToList(),
            p.Areas.Select(a => new AreaAtendimentoRequest
            {
                Cidade = a.Cidade,
                Bairro = a.Bairro
            }).ToList(),
            p.Disponibilidade.OrderBy(d => d.DiaSemana)
                .ThenBy(d => d.HoraInicio)
                .Select(d => new DisponibilidadeProfissionalRequest
                {
                    DiaSemana = d.DiaSemana,
                    HoraInicio = d.HoraInicio,
                    HoraFim = d.HoraFim,
                    AtendeImediato = d.AtendeImediato,
                    AtendeAgendado = d.AtendeAgendado
                }).ToList(),
            p.ExperienciaAnos,
            p.DistanciaMaxKm,
            p.AtendeUrgencia,
            p.AtendeDomicilio,
            p.AtendeEstabelecimento,
            p.Usuario.IdentidadeVerificada,
            verificacao);
    }

    public async Task<RespostaPaginada<AvaliacaoPublicaResponse>?> ListarAvaliacoesAsync(
        int id, PaginacaoQuery paginacao, CancellationToken ct = default)
    {
        var usuarioId = await _db.Profissionais.AsNoTracking()
            .Where(p => p.Id == id && p.Usuario.Ativo)
            .Select(p => (int?)p.UsuarioId)
            .FirstOrDefaultAsync(ct);

        if (usuarioId is null) return null;

        var consulta =
            from avaliacao in _db.Avaliacoes.AsNoTracking()
            join cliente in _db.Usuarios.AsNoTracking()
                on avaliacao.AvaliadorId equals cliente.Id
            where avaliacao.AvaliadoId == usuarioId.Value
            select new
            {
                avaliacao.Id,
                avaliacao.Nota,
                avaliacao.Comentario,
                NomeCliente = cliente.Nome,
                Data = avaliacao.CriadoEm
            };

        var total = await consulta.CountAsync(ct);

        var itens = await consulta.OrderByDescending(a => a.Data)
            .Skip((paginacao.Pagina - 1) * paginacao.Tamanho)
            .Take(paginacao.Tamanho)
            .Select(a => new AvaliacaoPublicaResponse(a.Id, a.Nota, a.Comentario, a.NomeCliente, a.Data))
            .ToListAsync(ct);

        return new RespostaPaginada<AvaliacaoPublicaResponse>
        {
            Itens = itens,
            Pagina = paginacao.Pagina,
            Tamanho = paginacao.Tamanho,
            Total = total
        };
    }

    public async Task<bool> AtualizarPerfilAsync(
        int usuarioId, AtualizarProfissionalRequest request, CancellationToken ct = default)
    {
        var p = await _db.Profissionais.Include(x => x.Usuario)
            .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId && x.Usuario.Ativo, ct);

        if (p is null) return false;

        p.Usuario.Nome = request.Nome.Trim();
        p.Usuario.Telefone = request.Telefone.Trim();
        p.DescricaoPerfil = request.Descricao.Trim();
        p.ExperienciaAnos = request.ExperienciaAnos;
        p.DistanciaMaxKm = request.DistanciaMaxKm;
        p.AtendeUrgencia = request.AtendeUrgencia;
        p.AtendeDomicilio = request.AtendeDomicilio;
        p.AtendeEstabelecimento = request.AtendeEstabelecimento;
        p.FotoUrl = string.IsNullOrWhiteSpace(request.FotoUrl)
            ? null
            : request.FotoUrl.Trim();

        p.FotosTrabalhos = request.FotosTrabalhos
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f => f.Trim())
            .Distinct(StringComparer.Ordinal)
            .Take(10)
            .ToList();

        await _db.SaveChangesAsync(ct);

        return true;
    }

    public async Task<bool> DefinirServicosAsync(
        int usuarioId, IReadOnlyList<ServicoProfissionalRequest> servicos,
        CancellationToken ct = default)
    {
        var profissionalId = await _db.Profissionais.AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId && p.Usuario.Ativo)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(ct);

        if (profissionalId is null) return false;

        var itens = servicos.GroupBy(x => x.CategoriaId)
            .Select(g => g.First())
            .ToList();

        var categoriasAtivas = await _db.Categorias.AsNoTracking()
            .Where(c => c.Ativa && itens.Select(x => x.CategoriaId).Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync(ct);

        if (categoriasAtivas.Count != itens.Count)
            throw BusinessException.Invalido(
                "CATEGORIA_INVALIDA",
                "Uma ou mais categorias não existem ou estão inativas.");

        var estrategia = _db.Database.CreateExecutionStrategy();

        await estrategia.ExecuteAsync(async () =>
        {
            await using var transacao =
                await _db.Database.BeginTransactionAsync(ct);

            var atuais = await _db.ProfissionalCategorias
                .Where(pc => pc.ProfissionalId == profissionalId.Value)
                .ToListAsync(ct);

            _db.ProfissionalCategorias.RemoveRange(atuais);

            foreach (var servico in itens)
            {
                _db.ProfissionalCategorias.Add(new ProfissionalCategoria
                {
                    ProfissionalId = profissionalId.Value,
                    CategoriaId = servico.CategoriaId,
                    FaixaPrecoMin = servico.FaixaPrecoMin,
                    FaixaPrecoMax = servico.FaixaPrecoMax
                });
            }

            await _db.SaveChangesAsync(ct);
            await transacao.CommitAsync(ct);
        });

        return true;
    }

    public async Task<bool> DefinirAreasAsync(
        int usuarioId, IReadOnlyList<AreaAtendimentoRequest> areas,
        CancellationToken ct = default)
    {
        var profissionalId = await _db.Profissionais.AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId && p.Usuario.Ativo)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(ct);

        if (profissionalId is null) return false;

        var novas = areas.Select(a => new AreaAtendimento
        {
            ProfissionalId = profissionalId.Value,
            Cidade = a.Cidade.Trim(),
            Bairro = string.IsNullOrWhiteSpace(a.Bairro)
                    ? null
                    : a.Bairro.Trim()
        })
            .GroupBy(a =>
                $"{Normalizar(a.Cidade)}|{Normalizar(a.Bairro ?? string.Empty)}")
            .Select(g => g.First())
            .ToList();

        var estrategia = _db.Database.CreateExecutionStrategy();

        await estrategia.ExecuteAsync(async () =>
        {
            await using var transacao =
                await _db.Database.BeginTransactionAsync(ct);

            var atuais = await _db.AreasAtendimento
                .Where(a => a.ProfissionalId == profissionalId.Value)
                .ToListAsync(ct);

            _db.AreasAtendimento.RemoveRange(atuais);

            await _db.AreasAtendimento.AddRangeAsync(novas, ct);
            await _db.SaveChangesAsync(ct);

            await transacao.CommitAsync(ct);
        });

        return true;
    }

    public async Task<bool> DefinirDisponibilidadeAsync(
        int usuarioId,
        IReadOnlyList<DisponibilidadeProfissionalRequest> disponibilidade,
        CancellationToken ct = default)
    {
        var profissionalId = await _db.Profissionais.AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId && p.Usuario.Ativo)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(ct);

        if (profissionalId is null) return false;

        var estrategia = _db.Database.CreateExecutionStrategy();

        await estrategia.ExecuteAsync(async () =>
        {
            await using var transacao =
                await _db.Database.BeginTransactionAsync(ct);

            var antigas = await _db.DisponibilidadesProfissionais
                .Where(x => x.ProfissionalId == profissionalId.Value)
                .ToListAsync(ct);

            _db.DisponibilidadesProfissionais.RemoveRange(antigas);

            await _db.DisponibilidadesProfissionais.AddRangeAsync(
                disponibilidade.Select(x => new DisponibilidadeProfissional
                {
                    ProfissionalId = profissionalId.Value,
                    DiaSemana = x.DiaSemana,
                    HoraInicio = x.HoraInicio,
                    HoraFim = x.HoraFim,
                    AtendeImediato = x.AtendeImediato,
                    AtendeAgendado = x.AtendeAgendado
                }), ct);

            await _db.SaveChangesAsync(ct);

            await transacao.CommitAsync(ct);
        });

        return true;
    }

    private async Task<(decimal Media, int Total)> ObterResumoAvaliacoesAsync(
        int usuarioId, CancellationToken ct)
    {
        var resumo = await _db.Avaliacoes.AsNoTracking()
            .Where(a => a.AvaliadoId == usuarioId)
            .GroupBy(a => a.AvaliadoId)
            .Select(g => new
            {
                Media = g.Average(a => a.Nota),
                Total = g.Count()
            })
            .FirstOrDefaultAsync(ct);

        return resumo is null
            ? (0m, 0)
            : (Math.Round((decimal)resumo.Media, 1), resumo.Total);
    }

    private static ProfissionalResumoResponse ParaResumo(
        Profissional p, int categoriaId, double? media, int total)
    {
        var categoria = p.Categorias.First(pc => pc.CategoriaId == categoriaId);

        return new ProfissionalResumoResponse(
            p.Id,
            p.Usuario.Nome,
            p.FotoUrl ?? p.Usuario.FotoUrl,
            p.Categorias.Where(pc => pc.Categoria.Ativa)
                .Select(pc => pc.Categoria.Nome)
                .Distinct()
                .ToList(),
            media is null ? 0m : Math.Round((decimal)media.Value, 1),
            total,
            p.Usuario.IdentidadeVerificada,
            categoria.FaixaPrecoMin ?? p.PrecoMin,
            categoria.FaixaPrecoMax ?? p.PrecoMax);
    }

    private static string Normalizar(string texto)
    {
        var decomposto = texto.Trim().ToLowerInvariant()
            .Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder(decomposto.Length);

        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) !=
                UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
