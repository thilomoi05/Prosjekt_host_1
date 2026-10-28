namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Én type utstyr som trengs, f.eks. "Nødstrøm (aggregat)", og hvor det trengs.
/// Viser ikke hvem som har meldt behovet, bare område og hva.
/// </summary>
public class UtstyrSomTrengs
{
    public BehovType Type { get; set; }

    // Navnet brukeren ser, f.eks. "Nødstrøm (aggregat)".
    public string Navn { get; set; } = string.Empty;

    // Hvor mange behov av denne typen som venter.
    public int Antall { get; set; }

    // Den høyeste prioriteten blant disse behovene. Styrer farge og rekkefølge.
    public Prioritet HoyestePrioritet { get; set; }

    // Områdene der det trengs, f.eks. "Lund", "Sentrum".
    public List<string> Omrader { get; set; } = new();
}
