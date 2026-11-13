namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Én rad i adminloggen (Views/Admin/Logg.cshtml).
/// Kategorien regnes ut fra hva hendelsen gjelder: et behov, en ressurs eller en bruker.
/// Hver kategori har sin egen farge, samme som på kartet (se "Adminloggen" i site.css).
/// </summary>
public class LoggRad
{
    public Hendelse Hendelse { get; set; } = null!;

    // "Behov", "Ressurs" eller "Brukere". Brukes i kategorifilteret og som merke i tabellen.
    // Hendelser uten behov og ressurs er godkjenning eller avvisning av brukere (AdminController).
    public string Kategori =>
        Hendelse.BehovId is not null ? "Behov"
        : Hendelse.RessursId is not null ? "Ressurs"
        : "Brukere";

    // CSS-klassen som gir kategorien farge: bp-logg-behov, bp-logg-ressurs eller bp-logg-brukere.
    public string Fargeklasse => "bp-logg-" + Kategori.ToLowerInvariant();
}
