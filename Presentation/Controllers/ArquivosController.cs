
using Chamapro.Api.Common;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chamapro.Api.Controllers;

[Authorize]
[Route(ApiRotas.Prefixo + "/arquivos")]
public class ArquivosController : ApiControllerBase
{
    private readonly IArquivoService _servico;

    public ArquivosController(IArquivoService servico)
    {
        _servico = servico;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<ArquivoResponse>> Enviar(
        IFormFile arquivo,
        CancellationToken ct)
    {
        var caminho = await _servico.SalvarImagemAsync(arquivo, ct);

        var url = $"{Request.Scheme}://{Request.Host}{caminho}";

        return Created(url, new ArquivoResponse(url));
    }
}
