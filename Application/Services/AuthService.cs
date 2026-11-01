using Chamapro.Api.Common;
using Chamapro.Api.Data;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Chamapro.Api.Services;

public interface IAuthService
{
    Task<LoginResponse> RegistrarClienteAsync(CadastroClienteRequest request, CancellationToken ct = default);
    Task<LoginResponse> RegistrarProfissionalAsync(CadastroProfissionalRequest request, CancellationToken ct = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UsuarioResumoResponse> ObterUsuarioAtualAsync(int usuarioId, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private static readonly string HashSenhaDesconhecida = BCrypt.Net.BCrypt.HashPassword("chamapro-dummy-login-password");
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;

    public AuthService(AppDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> RegistrarClienteAsync(CadastroClienteRequest request, CancellationToken ct = default)
        => await RegistrarAsync(request.Email, request.Nome, request.Senha, request.Telefone, TipoUsuario.Cliente, ct);

    public async Task<LoginResponse> RegistrarProfissionalAsync(CadastroProfissionalRequest request, CancellationToken ct = default)
        => await RegistrarAsync(request.Email, request.Nome, request.Senha, request.Telefone,
            TipoUsuario.Profissional, ct, request.DescricaoPerfil);

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = NormalizarEmail(request.Email);
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct);
        if (usuario is null || !usuario.Ativo)
        {
            BCrypt.Net.BCrypt.Verify(request.Senha, HashSenhaDesconhecida);
            throw BusinessException.Proibido("CREDENCIAIS_INVALIDADAS", "E-mail ou senha inválidos.");
        }

        var senhaValida = BCrypt.Net.BCrypt.Verify(request.Senha, usuario.SenhaHash);
        if (!senhaValida)
            throw BusinessException.Proibido("CREDENCIAIS_INVALIDADAS", "E-mail ou senha inválidos.");

        return new LoginResponse
        {
            Token = _tokenService.GerarToken(usuario),
            ExpiraEm = DateTime.UtcNow.AddHours(2),
            Usuario = MapearUsuario(usuario)
        };
    }

    public async Task<UsuarioResumoResponse> ObterUsuarioAtualAsync(int usuarioId, CancellationToken ct = default)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId, ct);
        if (usuario is null)
            throw BusinessException.NaoEncontrado("USUARIO_NAO_ENCONTRADO", "Usuário não encontrado.");

        return MapearUsuario(usuario);
    }

    private async Task<LoginResponse> RegistrarAsync(
        string email,
        string nome,
        string senha,
        string telefone,
        TipoUsuario tipo,
        CancellationToken ct,
        string? descricaoPerfil = null)
    {
        var emailNormalizado = NormalizarEmail(email);
        if (string.IsNullOrWhiteSpace(emailNormalizado))
            throw BusinessException.Invalido("EMAIL_OBRIGATORIO", "O e-mail é obrigatório.");

        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 8)
            throw BusinessException.Invalido("SENHA_INVALIDA", "A senha deve ter pelo menos 8 caracteres.");

        var jaExiste = await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == emailNormalizado, ct);
        if (jaExiste)
            throw BusinessException.Conflito("EMAIL_JA_CADASTRADO", "Este e-mail já está cadastrado.");

        var usuario = new Usuario
        {
            Nome = nome.Trim(),
            Email = emailNormalizado,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(senha),
            Tipo = tipo,
            Telefone = telefone.Trim(),
            Ativo = true,
            IdentidadeVerificada = false,
            CriadoEm = DateTime.UtcNow
        };

        if (tipo == TipoUsuario.Profissional)
        {
            var profissional = new Profissional
            {
                Usuario = usuario,
                DescricaoPerfil = descricaoPerfil?.Trim() ?? string.Empty,
                MediaAvaliacao = 0m
            };
            usuario.Profissional = profissional;
        }

        await _context.Usuarios.AddAsync(usuario, ct);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException postgres
            && postgres.SqlState == PostgresErrorCodes.UniqueViolation
            && postgres.ConstraintName is "ux_usuarios_email_lower" or "usuarios_email_key")
        {
            throw BusinessException.Conflito("EMAIL_JA_CADASTRADO", "Este e-mail já está cadastrado.");
        }

        return new LoginResponse
        {
            Token = _tokenService.GerarToken(usuario),
            ExpiraEm = DateTime.UtcNow.AddHours(2),
            Usuario = MapearUsuario(usuario)
        };
    }

    private static UsuarioResumoResponse MapearUsuario(Usuario usuario)
        => new()
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            Perfil = usuario.Tipo switch
            {
                TipoUsuario.Admin => Perfis.Admin,
                TipoUsuario.Profissional => Perfis.Profissional,
                _ => Perfis.Cliente
            },
            Ativo = usuario.Ativo
        };

    private static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();
}
