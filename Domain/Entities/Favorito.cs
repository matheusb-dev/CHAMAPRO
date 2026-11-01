namespace Chamapro.Api.Models.Entities;

public sealed class Favorito
{
    public int ClienteId { get; set; }
    public int ProfissionalId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public Usuario Cliente { get; set; } = null!;
    public Profissional Profissional { get; set; } = null!;
}
