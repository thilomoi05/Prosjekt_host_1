using Beredskapsportal.Services;

namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Siden der en offentlig aktør finner og forespør ressurser til sitt behov
/// (Views/Foresporsel/FinnRessurs.cshtml).
/// </summary>
public class FinnRessursViewModel
{
    public Behov Behov { get; set; } = new();

    // Ledige ressurser som passer, nærmeste først (se Tildelingsforslag).
    public List<RessursForslag> Forslag { get; set; } = new();

    // True: alle ledige ressurser vises, ikke bare de med passende type.
    public bool VisAlleTyper { get; set; }

    // Ressursene brukeren allerede har spurt om til dette behovet (venter på Admin).
    public HashSet<int> AlleredeForespurt { get; set; } = new();
}
