namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Alt detaljsiden for ett behov trenger (Views/Behov/Detaljer.cshtml):
/// selve behovet og ressursene som er tildelt det.
/// </summary>
public class BehovDetaljerViewModel
{
    public Behov Behov { get; set; } = new();

    // Ressursene som er satt inn på behovet. Tom liste hvis behovet venter.
    public List<Ressurs> TildelteRessurser { get; set; } = new();
}

/// <summary>
/// Alt detaljsiden for én ressurs trenger (Views/Ressurs/Detaljer.cshtml).
/// </summary>
public class RessursDetaljerViewModel
{
    public Ressurs Ressurs { get; set; } = new();

    // True hvis ressursen er satt inn på et behov.
    public bool ErTildelt { get; set; }

    // Behovet ressursen er tildelt. Bare fylt ut for offentlige aktører:
    // private aktører får bare vite AT ressursen er tildelt, ikke hvor.
    public Behov? TildeltBehov { get; set; }

    // True for offentlige aktører. Styrer lenker og tekst på siden.
    public bool ErOffentligAktor { get; set; }
}
