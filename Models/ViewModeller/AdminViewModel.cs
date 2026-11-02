using Beredskapsportal.Services;

namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Alt adminsiden (Views/Admin/Index.cshtml) trenger: de fire køene Admin jobber med.
/// </summary>
public class AdminViewModel
{
    // Forespørsler fra offentlige aktører om å få en ressurs til sitt behov, eldste først.
    public List<ForesporselRad> Foresporsler { get; set; } = new();

    // Nye ressurser som venter på godkjenning, eldste først (de har ventet lengst).
    public List<Ressurs> RessurserTilGodkjenning { get; set; } = new();

    // Behov som ikke har fått noen ressurs, med høy prioritet øverst.
    public List<VentendeBehov> BehovSomVenter { get; set; } = new();

    // Offentlige aktører som har registrert seg og venter på godkjenning.
    public List<Bruker> BrukereTilGodkjenning { get; set; } = new();
}

/// <summary>Ett behov som venter, og hvor mange passende ledige ressurser som finnes.</summary>
public class VentendeBehov
{
    public Behov Behov { get; set; } = new();
    public int AntallForslag { get; set; }
}

/// <summary>
/// Siden der Admin tildeler ressurser til ett behov (Views/Admin/Tildel.cshtml).
/// </summary>
public class TildelViewModel
{
    public Behov Behov { get; set; } = new();

    // Ressursene som allerede er tildelt behovet.
    public List<Ressurs> TildelteRessurser { get; set; } = new();

    // Ledige ressurser som foreslås, nærmeste først (se Tildelingsforslag).
    public List<RessursForslag> Forslag { get; set; } = new();

    // True: alle ledige ressurser vises, ikke bare de med passende type.
    public bool VisAlleTyper { get; set; }
}
