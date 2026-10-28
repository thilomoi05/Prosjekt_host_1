namespace Beredskapsportal.Models;

/// <summary>
/// Kobler en ressurs til et behov: "denne ressursen er satt inn på dette behovet".
/// Et behov kan få flere ressurser, og hver kobling er én Tildeling.
/// Tilsvarer tabellen Tildeling i datamodellen (IS-201), med (BehovId, RessursId) som nøkkel.
/// </summary>
public class Tildeling
{
    public int BehovId { get; set; }

    public int RessursId { get; set; }

    // Når ressursen ble tildelt.
    public DateTime Tidspunkt { get; set; }

    // Brukernavnet til den som tildelte ressursen (en offentlig aktør).
    public string TildeltAv { get; set; } = string.Empty;

    // Valgfri kommentar, f.eks. "Hentes av Heimevernet kl. 14".
    public string? Kommentar { get; set; }
}
