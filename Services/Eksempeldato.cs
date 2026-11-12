namespace Beredskapsportal.Services;

/// <summary>
/// Datoer i eksempeldataene, regnet ut fra dagens dato. Da ser demoen fersk ut
/// uansett når den kjøres: Dag(0) er i dag, Dag(-1) er i går, Dag(28) er om fire uker.
/// </summary>
internal static class Eksempeldato
{
    public static DateOnly Dag(int dager) => DateOnly.FromDateTime(DateTime.Today.AddDays(dager));

    public static DateTime Tid(int dager, int time, int minutt) =>
        DateTime.Today.AddDays(dager).AddHours(time).AddMinutes(minutt);
}