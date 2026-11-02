namespace Beredskapsportal.Models;

/// <summary>
/// En offentlig aktør spør om å få en ressurs til et av sine behov.
/// En administrator godkjenner (da blir ressursen tildelt) eller avslår.
/// Slik slipper administratoren å finne ressursene selv, men har fortsatt kontrollen.
/// Skrives "Foresporsel" uten ø i koden.
/// </summary>
public class Foresporsel
{
    public int Id { get; set; }

    public int BehovId { get; set; }

    public int RessursId { get; set; }

    // Brukernavnet til den som spurte (den som meldte behovet).
    public string ForespurtAv { get; set; } = string.Empty;

    public DateTime Tidspunkt { get; set; }

    // Valgfri kommentar til administratoren, f.eks. "Trengs før kl. 18".
    public string? Kommentar { get; set; }

    public ForesporselStatus Status { get; set; } = ForesporselStatus.Venter;

    // Hvorfor forespørselen ble avslått. Vises for den som spurte.
    public string? Begrunnelse { get; set; }

    // Brukernavnet til administratoren som behandlet den.
    public string? BehandletAv { get; set; }
}
