using Chamapro.Api.Common;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chamapro.Api.Controllers;

/// <summary>
/// Endpoints 9 a 14: vitrine pública de profissionais (busca, perfil e avaliações)
/// e edição do próprio perfil (só PROFISSIONAL).
/// </summary>
[Route(ApiRotas.Prefixo + "/profissionais")]
public class ProfissionaisController : ApiControllerBase
{
    private readonly IProfissionalService _servico;

    public ProfissionaisController(IProfissionalService servico) => _servico = servico;

    /// <summary>9 - GET /profissionais?categoriaId=&amp;cidade=: busca paginada (público).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<RespostaPaginada<ProfissionalResumoResponse>>> Buscar(
        [FromQuery] BuscaProfissionaisQuery filtro, CancellationToken ct)
        => Ok(await _servico.BuscarAsync(filtro, ct));

    /// <summary>10 - GET /profissionais/{id}: perfil público completo, sem e-mail nem telefone.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<PerfilPublicoResponse>> ObterPerfil(int id, CancellationToken ct)
        => Ok(await _servico.ObterPerfilAsync(id, ct));

    /// <summary>11 - GET /profissionais/{id}/avaliacoes: avaliações visíveis, paginadas (público).</summary>
    [HttpGet("{id:int}/avaliacoes")]
    [AllowAnonymous]
    public async Task<ActionResult<RespostaPaginada<AvaliacaoPublicaResponse>>> ListarAvaliacoes(
        int id, [FromQuery] PaginacaoQuery paginacao, CancellationToken ct)
        => Ok(await _servico.ListarAvaliacoesAsync(id, paginacao, ct));

    [HttpPut("me")]
    [Authorize(Roles = Perfis.Profissional)]
    public async Task<IActionResult> AtualizarPerfil([FromBody] AtualizarProfissionalRequest request, CancellationToken ct)
    {
        await _servico.AtualizarPerfilAsync(request, ct);
        return NoContent();
    }

    [HttpPut("me/servicos")]
    [Authorize(Roles = Perfis.Profissional)]
    public async Task<IActionResult> DefinirServicos([FromBody] List<ServicoProfissionalRequest> servicos, CancellationToken ct)
    {
        await _servico.DefinirServicosAsync(servicos, ct);
        return NoContent();
    }

    [HttpPut("me/areas")]
    [Authorize(Roles = Perfis.Profissional)]
    public async Task<IActionResult> DefinirAreas([FromBody] List<AreaAtendimentoRequest> areas, CancellationToken ct)
    {
        await _servico.DefinirAreasAsync(areas, ct);
        return NoContent();
    }
}