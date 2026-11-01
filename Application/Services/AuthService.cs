using Chamapro.Api.Common;
using Chamapro.Api.Data;
using Chamapro.Api.DTOs.Requests;
using Chamapro.Api.DTOs.Responses;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using Npgsql;

namespace Chamapro.Api.Services;

public interface IAuthService
{
    Task<LoginResponse> RegistrarClienteAsync(CadastroClienteRequest request, CancellationToken ct = default);
    Task<LoginResponse> RegistrarProfissionalAsync(CadastroProfissionalRequest request, CancellationToken ct = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task SolicitarRecuperacaoSenhaAsync(SolicitarRecuperacaoSenhaRequest request, CancellationToken ct = default);
    Task RedefinirSenhaAsync(RedefinirSenhaRequest request, CancellationToken ct = default);
    Task<UsuarioResumoResponse> ObterUsuarioAtualAsync(int usuarioId, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private static readonly string HashSenhaDesconhecida = BCrypt.Net.BCrypt.HashPassword("chamapro-dummy-login-password");
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext context,
        ITokenService tokenService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
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

    public async Task SolicitarRecuperacaoSenhaAsync(SolicitarRecuperacaoSenhaRequest request, CancellationToken ct = default)
    {
        var email = NormalizarEmail(request.Email);
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.Ativo, ct);
        if (usuario is null)
            return;

        var agora = DateTime.UtcNow;
        await _context.RecuperacoesSenha
            .Where(x => x.UsuarioId == usuario.Id && x.UsadoEm == null)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.UsadoEm, agora), ct);

        var (token, hash) = TokenRecuperacaoSenha.Criar();
        _context.RecuperacoesSenha.Add(new RecuperacaoSenha
        {
            UsuarioId = usuario.Id,
            TokenHash = hash,
            ExpiraEm = agora.AddMinutes(30)
        });
        await _context.SaveChangesAsync(ct);

        var publicUrl = _configuration["App:PublicUrl"];
        if (!Uri.TryCreate(publicUrl, UriKind.Absolute, out var baseUri)
            || baseUri.Scheme is not ("https" or "http")
            || (!baseUri.IsLoopback && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            _logger.LogWarning("Recuperação solicitada para usuário {UsuarioId}, mas App:PublicUrl não contém uma URL pública HTTPS válida.", usuario.Id);
            return;
        }

        var link = new Uri(baseUri, $"pages/autenticacao/recuperar-senha.html?token={WebUtility.UrlEncode(token)}");
        var nome = WebUtility.HtmlEncode(usuario.Nome);
        var href = WebUtility.HtmlEncode(link.AbsoluteUri);
        var corpo = $"<p>Olá, {nome}.</p><p>Recebemos uma solicitação para redefinir a senha da sua conta CHAMAPRO.</p><p><a href=\"{href}\">Criar uma nova senha</a></p><p>O link expira em 30 minutos e pode ser usado uma única vez. Se você não fez esta solicitação, ignore este e-mail.</p>";

        try
        {
            await _emailService.EnviarAsync(usuario.Email, "Redefinição de senha CHAMAPRO", corpo, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Não foi possível enviar o e-mail de recuperação para o usuário {UsuarioId}.", usuario.Id);
        }
    }

    public async Task RedefinirSenhaAsync(RedefinirSenhaRequest request, CancellationToken ct = default)
    {
        var hash = TokenRecuperacaoSenha.CalcularHash(request.Token);
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        var recuperacao = await _context.RecuperacoesSenha
            .FromSqlInterpolated($"SELECT * FROM recuperacoes_senha WHERE token_hash = {hash} FOR UPDATE")
            .SingleOrDefaultAsync(ct);

        var agora = DateTime.UtcNow;
        if (recuperacao is null || recuperacao.UsadoEm is not null || recuperacao.ExpiraEm <= agora)
            throw BusinessException.Invalido("TOKEN_RECUPERACAO_INVALIDO", "O link de recuperação é inválido ou expirou.");

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == recuperacao.UsuarioId && u.Ativo, ct);
        if (usuario is null)
            throw BusinessException.Invalido("TOKEN_RECUPERACAO_INVALIDO", "O link de recuperação é inválido ou expirou.");

        recuperacao.UsadoEm = agora;
        usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.NovaSenha);
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
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
