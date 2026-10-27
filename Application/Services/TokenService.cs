using System.Security.Claims;
using System.Text;
using Chamapro.Api.Common;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Chamapro.Api.Services;

public interface ITokenService
{
    string GerarToken(Usuario usuario);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GerarToken(Usuario usuario)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "CHAMAPRO-DEV-KEY-CHANGE-ME-2026-!@#";
        var issuer = _configuration["Jwt:Issuer"] ?? "Chamapro.Api";
        var audience = _configuration["Jwt:Audience"] ?? "Chamapro.Clients";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new("sub", usuario.Id.ToString()),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Role, TipoParaPerfil(usuario.Tipo))
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string TipoParaPerfil(TipoUsuario tipo)
        => tipo switch
        {
            TipoUsuario.Admin => Perfis.Admin,
            TipoUsuario.Profissional => Perfis.Profissional,
            _ => Perfis.Cliente
        };
}
