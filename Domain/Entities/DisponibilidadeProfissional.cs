namespace Chamapro.Api.Models.Entities;

public sealed class DisponibilidadeProfissional
{
    public int ProfissionalId { get; set; }
    public short DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFim { get; set; }
    public bool AtendeImediato { get; set; }
    public bool AtendeAgendado { get; set; } = true;
    public Profissional Profissional { get; set; } = null!;
}
