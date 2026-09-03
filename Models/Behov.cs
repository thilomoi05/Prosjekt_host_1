namespace Beredskapsportal.Models;

/// <summary>
/// Representerer et akutt eller planlagt behov meldt inn av en offentlig aktør
/// (kommune, sykehus, nødetat) under strømbruddet i Kristiansand.
/// </summary>
public class Behov
{
    public int Id { get; set; }

    public BehovType Type { get; set; }

    public string Beskrivelse { get; set; } = string.Empty;

    public string GeografiskOmrade { get; set; } = string.Empty;

    public Prioritet Prioritet { get; set; }

    public BehovStatus Status { get; set; } = BehovStatus.Ny;

    public string Kontaktperson { get; set; } = string.Empty;

    public string Telefon { get; set; } = string.Empty;

    public DateOnly Dato { get; set; }

    /// <summary>
    /// Teksten som skal vises for behovstypen i grensesnittet, f.eks. "Nødstrøm (aggregat)".
    /// Holder visningsnavn samlet ett sted i stedet for å spres utover Views.
    /// </summary>
    public string VisningsNavn => Type switch
    {
        BehovType.Nodstrom => "Nødstrøm (aggregat)",
        BehovType.Drivstofftransport => "Drivstofftransport",
        BehovType.Kommunikasjonsutstyr => "Kommunikasjonsutstyr",
        BehovType.Nodbelysning => "Nødbelysning",
        BehovType.Oppvarming => "Oppvarming (varmeovner)",
        _ => "Annet"
    };
}
