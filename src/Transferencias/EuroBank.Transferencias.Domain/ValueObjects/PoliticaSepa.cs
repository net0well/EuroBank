namespace EuroBank.Transferencias.Domain.ValueObjects;

/// <summary>
/// Horário de corte das transferências SEPA (destino em EUR): pedidas depois das 16:00 UTC, ou no sábado e no
/// domingo, executam no próximo dia útil às 08:00 UTC. Pode ser desligado por configuração (os testes ponta a ponta
/// não podem depender da hora em que rodam).
/// </summary>
public sealed record PoliticaSepa
{
    public static PoliticaSepa Padrao { get; } = new();

    public bool AplicarHorarioDeCorte { get; init; } = true;

    public TimeOnly HorarioDeCorte { get; init; } = new(16, 0);

    public TimeOnly Abertura { get; init; } = new(8, 0);

    public DateTimeOffset ExecucaoEfetiva(DateTimeOffset pedido)
    {
        var utc = pedido.ToUniversalTime();
        if (!AplicarHorarioDeCorte || (EhDiaUtil(utc.DayOfWeek) && TimeOnly.FromTimeSpan(utc.TimeOfDay) < HorarioDeCorte))
        {
            return pedido;
        }

        var dia = DateOnly.FromDateTime(utc.UtcDateTime).AddDays(1);
        while (!EhDiaUtil(dia.DayOfWeek))
        {
            dia = dia.AddDays(1);
        }

        return new DateTimeOffset(dia.ToDateTime(Abertura), TimeSpan.Zero);
    }

    private static bool EhDiaUtil(DayOfWeek dia) => dia is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
}
