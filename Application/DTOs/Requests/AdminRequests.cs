using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public sealed class AdminUsuarioStatusRequest
{
    public bool Ativo { get; set; }
}

public sealed class AdminUsuariosQuery
{
    [StringLength(100)]
    public string? Busca { get; set; }

    [StringLength(20)]
    public string? Perfil { get; set; }

    public bool? Ativo { get; set; }
}
