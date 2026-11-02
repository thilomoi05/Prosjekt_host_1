namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Én rad på siden "Alle tildelte ressurser" (Views/Tildeling/Index.cshtml):
/// ett behov, én ressurs som er satt inn på det, og selve tildelingen (når og av hvem).
/// Et behov med to ressurser blir altså to rader.
/// </summary>
public class TildelingRad
{
    public Behov Behov { get; set; } = new();
    public Ressurs Ressurs { get; set; } = new();
    public Tildeling Tildeling { get; set; } = new();
}
