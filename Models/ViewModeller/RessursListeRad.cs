namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Én rad på siden med alle ressurser (Views/Ressurs/Index.cshtml):
/// selve ressursen, om den er tildelt et behov, og om den innloggede brukeren eier den.
/// Status, tekst og farge regnes ut her, så viewet slipper å inneholde logikk.
/// Tekstene og fargene er de samme som på Oversikt og Ressurs/Detaljer.
/// </summary>
public class RessursListeRad
{
    public Ressurs Ressurs { get; set; } = new();

    // True hvis ressursen finnes i Tildelinger, altså er satt inn på et behov.
    public bool ErTildelt { get; set; }

    // True hvis den innloggede brukeren registrerte ressursen.
    public bool ErMin { get; set; }

    /// <summary>
    /// Kortnavn for statusen, brukt av filteret i tabellfilter.js (data-status).
    /// En godkjent ressurs er enten tildelt eller tilgjengelig.
    /// </summary>
    public string StatusKode => Ressurs.Status switch
    {
        RessursStatus.Venter => "venter",
        RessursStatus.Avvist => "avvist",
        _ => ErTildelt ? "tildelt" : "tilgjengelig"
    };

    /// <summary>Teksten som vises i status-merket.</summary>
    public string StatusTekst => StatusKode switch
    {
        "venter" => "Venter på godkjenning",
        "avvist" => "Avvist",
        "tildelt" => "Tildelt et behov",
        _ => "Tilgjengelig"
    };

    /// <summary>CSS-klassen til status-merket (fargene står i site.css).</summary>
    public string StatusCss => StatusKode switch
    {
        "venter" => "bp-badge-godkjenning",
        "avvist" => "bp-badge-avvist",
        "tildelt" => "bp-badge-tildelt",
        _ => "bp-badge-venter"
    };
}
