namespace Biblioteca.Tests;

/// <summary>Relógio controlado pelo teste: permite "viajar no tempo" sem esperar dias de verdade.</summary>
internal sealed class RelogioFalso : TimeProvider
{
    private DateTimeOffset _agora;

    public RelogioFalso(DateOnly dia)
    {
        _agora = new DateTimeOffset(dia.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);
    }

    public override DateTimeOffset GetUtcNow() => _agora;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public void AvancarDias(int dias) => _agora = _agora.AddDays(dias);
}
