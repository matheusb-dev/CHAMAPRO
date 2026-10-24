using System.Globalization;
using System.Text;
using Chamapro.Api.Common;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;

namespace Chamapro.Api.Repositories;

public class ProfissionalRepository : IProfissionalRepository
{
    private readonly ICategoriaRepository _categorias;

    public ProfissionalRepository(ICategoriaRepository categorias) => _categorias = categorias;

    private record ServicoDados(int CategoriaId, decimal Min, decimal Max);
    private record AreaDados(string Cidade, string? Bairro);
    private record AvaliacaoDados(int Nota, string? Comentario, string NomeCliente, DateTime Data);

    private class ProfissionalDados
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? FotoUrl { get; set; }
        public string? Descricao { get; set; }
        public string? Telefone { get; set; }
        public bool Verificado { get; set; }
        public bool Ativo { get; set; } = true;
        public DateTime MembroDesde { get; set; }
        public List<ServicoDados> Servicos { get; set; } = new();
        public List<AreaDados> Areas { get; set; } = new();
        public List<string> FotosTrabalhos { get; set; } = new();
        public List<AvaliacaoDados> Avaliacoes { get; set; } = new();
    }

    private static DateTime D(int ano, int mes, int dia) => new(ano, mes, dia, 0, 0, 0, DateTimeKind.Utc);

    private static readonly object Trava = new();

    private static readonly List<ProfissionalDados> Dados = new()
    {
        new ProfissionalDados
        {
            Id = 1, UsuarioId = 2, Nome = "Carlos Silva",
            Descricao = "Eletricista residencial com 10 anos de experiência.",
            Telefone = "44999990001", Verificado = true, MembroDesde = D(2026, 1, 15),
            Servicos = new() { new ServicoDados(1, 80m, 250m) },
            Areas = new() { new AreaDados("Maringá", null) },
            Avaliacoes = new()
            {
                new AvaliacaoDados(5, "Serviço rápido e limpo.", "Joana", D(2026, 8, 3)),
                new AvaliacaoDados(4, "Bom profissional, chegou no horário.", "Pedro", D(2026, 9, 12))
            }
        },
        new ProfissionalDados
        {
            Id = 2, UsuarioId = 3, Nome = "Marcos Souza",
            Descricao = "Encanador, trabalho com vazamentos e instalações.",
            Telefone = "44999990002", Verificado = false, MembroDesde = D(2026, 2, 20),
            Servicos = new() { new ServicoDados(2, 100m, 300m) },
            Areas = new() { new AreaDados("Maringá", null) },
            Avaliacoes = new()
            {
                new AvaliacaoDados(5, "Resolveu o vazamento no mesmo dia.", "Maria", D(2026, 7, 21)),
                new AvaliacaoDados(3, "Bom serviço, mas atrasou.", "Lucas", D(2026, 9, 2)),
                new AvaliacaoDados(4, null, "Paula", D(2026, 10, 1))
            }
        },
        new ProfissionalDados
        {
            Id = 3, UsuarioId = 4, Nome = "Ana Pereira",
            Descricao = "Eletricista, instalações e manutenção.",
            Telefone = "44999990003", Verificado = false, MembroDesde = D(2026, 3, 10),
            Servicos = new() { new ServicoDados(1, 70m, 200m) },
            Areas = new() { new AreaDados("Maringá", "Zona 7"), new AreaDados("Sarandi", null) },
            Avaliacoes = new()
            {
                new AvaliacaoDados(5, "Excelente!", "Rita", D(2026, 8, 15)),
                new AvaliacaoDados(5, "Muito atenciosa.", "João", D(2026, 9, 5)),
                new AvaliacaoDados(4, null, "Bia", D(2026, 9, 28)),
                new AvaliacaoDados(5, "Recomendo.", "Tiago", D(2026, 10, 10))
            }
        }
    };

    public async Task<RespostaPaginada<ProfissionalResumoResponse>> BuscarAsync(
        BuscaProfissionaisQuery filtro, CancellationToken ct = default)
    {
        var categoriaId = filtro.CategoriaId ?? 0;
        var cidade = Normalizar(filtro.Cidade ?? string.Empty);
        var bairro = string.IsNullOrWhiteSpace(filtro.Bairro) ? null : Normalizar(filtro.Bairro);
        var nomes = await NomesDasCategoriasAsync(ct);

        List<ProfissionalResumoResponse> itens;
        lock (Trava)
        {
            itens = Dados
                .Where(p => p.Ativo
                    && p.Servicos.Count > 0 && p.Areas.Count > 0 // cadastro completo
                    && p.Servicos.Any(s => s.CategoriaId == categoriaId)
                    && p.Areas.Any(a => Normalizar(a.Cidade) == cidade
                        && (bairro is null || a.Bairro is null || Normalizar(a.Bairro) == bairro))
                    && (filtro.Verificado != true || p.Verificado)
                    && (filtro.NotaMinima is null || Media(p) >= filtro.NotaMinima.Value))
                .Select(p => ParaResumo(p, categoriaId, nomes))
                .ToList();
        }

        IEnumerable<ProfissionalResumoResponse> ordenados = filtro.Ordenar switch
        {
            "avaliacoes" => itens.OrderByDescending(i => i.TotalAvaliacoes)
                                 .ThenByDescending(i => i.NotaMedia).ThenBy(i => i.Nome),
            "preco" => itens.OrderBy(i => i.FaixaPrecoMin ?? decimal.MaxValue)
                            .ThenByDescending(i => i.NotaMedia).ThenBy(i => i.Nome),
            _ => itens.OrderByDescending(i => i.NotaMedia)
                      .ThenByDescending(i => i.TotalAvaliacoes).ThenBy(i => i.Nome)
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
            Total = itens.Count
        };
    }

    public async Task<PerfilPublicoResponse?> ObterPerfilPublicoAsync(int id, CancellationToken ct = default)
    {
        var nomes = await NomesDasCategoriasAsync(ct);

        lock (Trava)
        {
            var p = Dados.FirstOrDefault(x => x.Id == id && x.Ativo);
            if (p is null) return null;

            return new PerfilPublicoResponse(
                p.Id,
                p.Nome,
                p.FotoUrl,
                p.Descricao,
                p.Servicos.Select(s => new EspecialidadeResponse(NomeCategoria(nomes, s.CategoriaId), s.Min, s.Max)).ToList(),
                p.Areas.Select(a => a.Bairro is null ? a.Cidade : $"{a.Cidade} - {a.Bairro}").ToList(),
                Media(p),
                p.Avaliacoes.Count,
                p.Verificado,
                p.FotosTrabalhos.ToList(),
                p.MembroDesde);
        }
    }

    public Task<RespostaPaginada<AvaliacaoPublicaResponse>?> ListarAvaliacoesAsync(
        int id, PaginacaoQuery paginacao, CancellationToken ct = default)
    {
        lock (Trava)
        {
            var p = Dados.FirstOrDefault(x => x.Id == id && x.Ativo);
            if (p is null)
                return Task.FromResult<RespostaPaginada<AvaliacaoPublicaResponse>?>(null);

            var ordenadas = p.Avaliacoes.OrderByDescending(a => a.Data).ToList();
            var itens = ordenadas
                .Skip((paginacao.Pagina - 1) * paginacao.Tamanho)
                .Take(paginacao.Tamanho)
                .Select(a => new AvaliacaoPublicaResponse(a.Nota, a.Comentario, a.NomeCliente, a.Data))
                .ToList();

            var resposta = new RespostaPaginada<AvaliacaoPublicaResponse>
            {
                Itens = itens,
                Pagina = paginacao.Pagina,
                Tamanho = paginacao.Tamanho,
                Total = ordenadas.Count
            };
            return Task.FromResult<RespostaPaginada<AvaliacaoPublicaResponse>?>(resposta);
        }
    }

    public Task<bool> AtualizarPerfilAsync(int usuarioId, AtualizarProfissionalRequest request, CancellationToken ct = default)
    {
        lock (Trava)
        {
            var p = Dados.FirstOrDefault(x => x.UsuarioId == usuarioId);
            if (p is null) return Task.FromResult(false);

            p.Nome = request.Nome.Trim();
            p.Descricao = request.Descricao.Trim();
            p.Telefone = request.Telefone;
            p.FotoUrl = string.IsNullOrWhiteSpace(request.FotoUrl) ? null : request.FotoUrl.Trim();
            p.FotosTrabalhos = request.FotosTrabalhos.Where(f => !string.IsNullOrWhiteSpace(f)).ToList();
            return Task.FromResult(true);
        }
    }

    public Task<bool> DefinirServicosAsync(int usuarioId, IReadOnlyList<ServicoProfissionalRequest> servicos, CancellationToken ct = default)
    {
        lock (Trava)
        {
            var p = Dados.FirstOrDefault(x => x.UsuarioId == usuarioId);
            if (p is null) return Task.FromResult(false);

            p.Servicos = servicos
                .Select(s => new ServicoDados(s.CategoriaId, s.FaixaPrecoMin, s.FaixaPrecoMax))
                .ToList();
            return Task.FromResult(true);
        }
    }

    public Task<bool> DefinirAreasAsync(int usuarioId, IReadOnlyList<AreaAtendimentoRequest> areas, CancellationToken ct = default)
    {
        lock (Trava)
        {
            var p = Dados.FirstOrDefault(x => x.UsuarioId == usuarioId);
            if (p is null) return Task.FromResult(false);

            var novas = new List<AreaDados>();
            foreach (var a in areas)
            {
                var cidade = a.Cidade.Trim();
                var bairro = string.IsNullOrWhiteSpace(a.Bairro) ? null : a.Bairro.Trim();
                var repetida = novas.Any(x => Normalizar(x.Cidade) == Normalizar(cidade)
                                           && Normalizar(x.Bairro ?? string.Empty) == Normalizar(bairro ?? string.Empty));
                if (!repetida) novas.Add(new AreaDados(cidade, bairro));
            }

            p.Areas = novas;
            return Task.FromResult(true);
        }
    }

    private async Task<Dictionary<int, string>> NomesDasCategoriasAsync(CancellationToken ct)
    {
        var categorias = await _categorias.ListarAtivasAsync(ct);
        return categorias.ToDictionary(c => c.Id, c => c.Nome);
    }

    private static string NomeCategoria(Dictionary<int, string> nomes, int categoriaId)
        => nomes.TryGetValue(categoriaId, out var nome) ? nome : $"Categoria {categoriaId}";

    private static decimal Media(ProfissionalDados p)
        => p.Avaliacoes.Count == 0 ? 0m : Math.Round((decimal)p.Avaliacoes.Average(a => a.Nota), 1);

    private static ProfissionalResumoResponse ParaResumo(
        ProfissionalDados p, int categoriaId, Dictionary<int, string> nomes)
    {
        var servico = p.Servicos.First(s => s.CategoriaId == categoriaId);
        return new ProfissionalResumoResponse(
            p.Id,
            p.Nome,
            p.FotoUrl,
            p.Servicos.Select(s => NomeCategoria(nomes, s.CategoriaId)).ToList(),
            Media(p),
            p.Avaliacoes.Count,
            p.Verificado,
            servico.Min,
            servico.Max);
    }

    private static string Normalizar(string texto)
    {
        var decomposto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposto.Length);
        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}