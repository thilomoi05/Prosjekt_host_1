namespace Beredskapsportal.Models;

/// <summary>
/// Tekstene som skrives i adminloggen når noe skjer med en konto: innlogging, utlogging,
/// registrering og passordbytte. Samlet her, så teksten er lik overalt, og så adminloggen
/// (Views/Admin/Logg.cshtml) kan kjenne igjen advarslene med ErAdvarsel.
///
/// Hendelsene lagres i samme tabell som historikken til behov og ressurser (Hendelser),
/// men uten BehovId og RessursId. Derfor trengs ingen ny tabell eller migration.
/// </summary>
public static class Kontohendelser
{
    public const string LoggetInn = "Logget inn";
    public const string LoggetUt = "Logget ut";
    public const string FeilPassord = "Mislykket innlogging: feil passord";
    public const string UkjentBrukernavn = "Mislykket innlogging: ukjent brukernavn";
    public const string KontoLast = "Innlogging stoppet: kontoen er låst etter for mange feil forsøk";
    public const string VenterPaGodkjenning = "Innlogging stoppet: kontoen venter på godkjenning";
    public const string RegistrertPrivat = "Ny konto registrert som privat aktør";
    public const string RegistrertOffentlig = "Ny konto registrert som offentlig aktør, venter på godkjenning";
    public const string PassordEndret = "Byttet passord";

    // Står som "utført av" når brukernavnet ikke finnes. Det som ble skrevet inn, lagres ikke:
    // folk skriver av og til passordet sitt i brukernavnfeltet ved en feil.
    public const string UkjentBruker = "(ukjent)";

    // Hendelsene som kan tyde på at noen prøver å gjette passord. Markeres med rødt i loggen.
    private static readonly HashSet<string> Advarsler = new() { FeilPassord, UkjentBrukernavn, KontoLast };

    public static bool ErAdvarsel(string beskrivelse) => Advarsler.Contains(beskrivelse);
}
