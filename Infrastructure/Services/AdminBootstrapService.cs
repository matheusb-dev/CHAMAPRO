using System.ComponentModel.DataAnnotations;
using Chamapro.Api.Data;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Chamapro.Api.Services;

/// <summary>Provisiona o primeiro administrador somente quando credenciais explícitas estão configuradas.</summary>
public sealed class AdminBootstrapService : IHostedService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminBootstrapService> _logger;

    public AdminBootstrapService(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<AdminBootstrapService> logger)
    {
        _scopes = scopes;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var email = _configuration["Bootstrap:AdminEmail"]?.Trim().ToLowerInvariant();
        var senha = _configuration["Bootstrap:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(senha))
        {
            _logger.LogInformation("Bootstrap do administrador não configurado; nenhuma conta foi criada.");
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
            throw new InvalidOperationException("Bootstrap:AdminEmail precisa conter um endereço de e-mail válido.");
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 16 || senha.Length > 128)
            throw new InvalidOperationException("Bootstrap:AdminPassword precisa ter entre 16 e 128 caracteres.");

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existente = await db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);
        if (existente is not null)
        {
            if (existente.Tipo != TipoUsuario.Admin)
                throw new InvalidOperationException("Bootstrap:AdminEmail já pertence a uma conta que não é administradora.");
            _logger.LogInformation("A conta administrativa de bootstrap já existe; nenhuma credencial foi alterada.");
            return;
        }

        db.Usuarios.Add(new Usuario
        {
            Nome = "Administrador",
            Email = email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(senha),
            Tipo = TipoUsuario.Admin,
            Ativo = true,
            IdentidadeVerificada = true,
            CriadoEm = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Conta administrativa inicial criada a partir da configuração segura de bootstrap.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
