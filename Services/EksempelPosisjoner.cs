namespace Beredskapsportal.Services;

/// <summary>
/// Gir omtrentlige kartkoordinater til eksempeldataene i InMemory-repositoriene,
/// slik at kartet viser noe helt fra start.
/// Ekte behov og ressurser får plasseringen sin ved at brukeren klikker i kartet
/// i registreringsskjemaet, så denne klassen brukes bare til eksempeldata.
/// Kan slettes når vi bytter til MariaDB med ekte data.
/// </summary>
public static class EksempelPosisjoner
{
    // Omtrentlig midtpunkt i noen områder i Kristiansand (breddegrad, lengdegrad).
    private static readonly Dictionary<string, (double Breddegrad, double Lengdegrad)> Omrader = new()
    {
        ["Sentrum"] = (58.1467, 7.9956),
        ["Lund"] = (58.1500, 8.0200),
        ["Vågsbygd"] = (58.1250, 7.9400),
        ["Grim"] = (58.1560, 7.9780),
        ["Eg"] = (58.1650, 7.9930),
        ["Randesund"] = (58.1320, 8.0850),
        ["Songdalen"] = (58.1860, 7.8300),
        ["Søm"] = (58.1470, 8.0700),
    };

    /// <summary>
    /// Henter en plassering i det gitte området. "nummer" brukes til å flytte
    /// punktet litt, slik at flere punkter i samme område ikke havner oppå hverandre.
    /// </summary>
    public static (double Breddegrad, double Lengdegrad) For(string omrade, int nummer)
    {
        // Ukjent område: bruk sentrum.
        var (bredde, lengde) = Omrader.GetValueOrDefault(omrade, Omrader["Sentrum"]);

        // Enkel "spredning": gir et tall mellom -4 og 4 ut fra nummeret,
        // og flytter punktet noen hundre meter i hver retning.
        var forskyvningNord = (nummer * 7 % 9) - 4;
        var forskyvningOst = (nummer * 5 % 9) - 4;

        return (bredde + forskyvningNord * 0.0015, lengde + forskyvningOst * 0.003);
    }
}
