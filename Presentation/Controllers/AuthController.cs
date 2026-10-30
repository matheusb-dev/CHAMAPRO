using System.Security.Claims;
using Chamapro.Api.Common;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chamapro.Api.Controllers;

[Route(ApiRotas.Prefixo + "/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("cadastro/cliente")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> CadastrarCliente(
        [FromBody] CadastroClienteRequest request,
        CancellationToken ct)
    {
        var resposta = await _authService.RegistrarClienteAsync(request, ct);
        return Created("/api/v1/auth/me", resposta);
    }

    [HttpPost("cadastro/profissional")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> CadastrarProfissional(
        [FromBody] CadastroProfissionalRequest request,
        CancellationToken ct)
    {
        var resposta = await _authService.RegistrarProfissionalAsync(request, ct);
        return Created("/api/v1/auth/me", resposta);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var resposta = await _authService.LoginAsync(request, ct);
        return Ok(resposta);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UsuarioResumoResponse>> Me(CancellationToken ct)
    {
        var userIdRaw = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdRaw, out var userId) || userId <= 0)
            throw BusinessException.Proibido("NAO_AUTENTICADO", "Usuário não autenticado.");

        var resposta = await _authService.ObterUsuarioAtualAsync(userId, ct);
        return Ok(resposta);
    }
}