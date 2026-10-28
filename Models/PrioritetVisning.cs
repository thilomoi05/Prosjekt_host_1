namespace Beredskapsportal.Models;

/// <summary>
/// Hjelpemetoder for å vise prioriteten til et behov på skjermen.
/// Samlet ett sted, så alle sider viser samme tekst og farge.
/// </summary>
public static class PrioritetVisning
{
    /// <summary>Teksten brukeren ser, f.eks. "Høy" (i koden heter verdien Hoy, uten ø).</summary>
    public static string Tekst(Prioritet prioritet) => prioritet switch
    {
        Prioritet.Hoy => "Høy",
        Prioritet.Moderat => "Moderat",
        _ => "Lav"
    };

    /// <summary>
    /// Navnet som brukes i CSS-klassene, f.eks. "hoy" i "bp-ikon-hoy" og "bp-badge-hoy".
    /// Fargene står i site.css.
    /// </summary>
    public static string CssNavn(Prioritet prioritet) => prioritet switch
    {
        Prioritet.Hoy => "hoy",
        Prioritet.Moderat => "moderat",
        _ => "lav"
    };
}
