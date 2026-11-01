using System.ComponentModel.DataAnnotations;

namespace Chamapro.Api.DTOs.Requests;

public sealed class DisponibilidadeProfissionalRequest : IValidatableObject
{
    [Range(0, 6, ErrorMessage = "Dia da semana inválido (0=domingo a 6=sábado).")]
    public short DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFim { get; set; }
    public bool AtendeImediato { get; set; }
    public bool AtendeAgendado { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (HoraFim <= HoraInicio)
            yield return new ValidationResult("O horário final deve ser posterior ao inicial.", [nameof(HoraFim)]);
        if (!AtendeImediato && !AtendeAgendado)
            yield return new ValidationResult("Marque atendimento imediato ou agendado.", [nameof(AtendeImediato)]);
    }
}
