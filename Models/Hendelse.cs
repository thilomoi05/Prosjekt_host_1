namespace Beredskapsportal.Models;

/// <summary>
/// Én linje i historikken til et behov eller en ressurs, f.eks.
/// "02.11 14:05 · Godkjent av admin" eller "02.11 14:20 · Tildelt behov #5 av admin".
/// Vises på detaljsidene, så man kan se hva som har skjedd og hvem som gjorde det.
///
/// BehovId og RessursId er ikke fremmednøkler. Historikken skal bli liggende
/// selv om behovet eller ressursen trekkes tilbake (slettes).
/// </summary>
public class Hendelse
{
    public int Id { get; set; }

    public DateTime Tidspunkt { get; set; }

    // Brukernavnet til den som gjorde det.
    public string UtfortAv { get; set; } = string.Empty;

    // Hvilket behov eller hvilken ressurs hendelsen gjelder. Begge er tomme når hendelsen
    // gjelder en bruker, f.eks. når admin godkjenner en offentlig aktør (se adminloggen på /Admin/Logg).
    public int? BehovId { get; set; }
    public int? RessursId { get; set; }

    // Hva som skjedde, f.eks. "Ressursen ble godkjent".
    public string Beskrivelse { get; set; } = string.Empty;
}
