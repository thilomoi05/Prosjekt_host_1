namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Én forespørsel slik den vises på adminsiden og på detaljsiden til behovet:
/// selve forespørselen, behovet, ressursen og avstanden mellom dem.
/// Behov eller Ressurs er null hvis de er trukket tilbake.
/// </summary>
public class ForesporselRad
{
    public Foresporsel Foresporsel { get; set; } = new();
    public Behov? Behov { get; set; }
    public Ressurs? Ressurs { get; set; }
    public double AvstandKm { get; set; }
}
