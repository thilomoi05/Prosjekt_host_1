namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Én rad på siden "Alle ressurser" (Views/Ressurs/Index.cshtml):
/// selve ressursen, og om den er tildelt et behov. Godkjenningsstatusen står i Ressurs.Status.
/// Samme oppbygning som MinRessurs på Oversikt.
/// </summary>
public class RessursRad
{
    public Ressurs Ressurs { get; set; } = new();
    public bool ErTildelt { get; set; }
}
