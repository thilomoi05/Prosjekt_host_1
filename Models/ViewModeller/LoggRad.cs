namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Én rad i adminloggen (Views/Admin/Logg.cshtml).
/// Kategorien regnes ut fra hva hendelsen gjelder: et behov, en ressurs eller en konto.
/// </summary>
public class LoggRad
{
    public Hendelse Hendelse { get; set; } = null!;

    // "Behov", "Ressurs" eller "Konto". Brukes i kategorifilteret og som merke i tabellen.
    public string Kategori =>
        Hendelse.BehovId is not null ? "Behov"
        : Hendelse.RessursId is not null ? "Ressurs"
        : "Konto";

    // Feil passord, ukjent brukernavn eller låst konto (se Models/Kontohendelser.cs).
    public bool ErAdvarsel => Kontohendelser.ErAdvarsel(Hendelse.Beskrivelse);
}
