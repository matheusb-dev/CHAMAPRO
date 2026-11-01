namespace Chamapro.Api.Models.Time;

/// <summary>Converte timestamps armazenados em UTC para o horário local usado na agenda do produto.</summary>
public static class HorarioBrasil
{
    private static readonly Lazy<TimeZoneInfo> Zona = new(() =>
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
    });

    public static DateTime AgoraLocal()
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona.Value);

    public static DateTime ParaLocal(DateTime dataUtc)
    {
        var utc = dataUtc.Kind switch
        {
            DateTimeKind.Utc => dataUtc,
            DateTimeKind.Local => dataUtc.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dataUtc, DateTimeKind.Utc)
        };
        return TimeZoneInfo.ConvertTimeFromUtc(utc, Zona.Value);
    }
}
