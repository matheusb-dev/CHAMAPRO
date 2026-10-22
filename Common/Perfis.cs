namespace Chamapro.Api.Common;

/// <summary>Nomes dos perfis (roles) usados em [Authorize(Roles = ...)] e nas claims.</summary>
public static class Perfis
{
    public const string Cliente = "CLIENTE";
    public const string Profissional = "PROFISSIONAL";
    public const string Admin = "ADMIN";

    public static readonly string[] Todos = { Cliente, Profissional, Admin };
}
