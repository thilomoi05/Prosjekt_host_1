using System.ComponentModel.DataAnnotations;

namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Data som sendes inn fra skjemaet "Registrer nytt behov".
/// </summary>
public class BehovViewModel
{
    [Required(ErrorMessage = "Du må velge type behov.")]
    [Display(Name = "Type behov")]
    public BehovType? Type { get; set; }

    [Required(ErrorMessage = "Du må beskrive behovet.")]
    [Display(Name = "Beskrivelse")]
    public string Beskrivelse { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du må angi geografisk område.")]
    [Display(Name = "Geografisk område")]
    public string GeografiskOmrade { get; set; } = string.Empty;

    [Display(Name = "Prioritet")]
    public Prioritet Prioritet { get; set; } = Prioritet.Planlagt;

    [Required(ErrorMessage = "Du må oppgi en kontaktperson.")]
    [Display(Name = "Kontaktperson")]
    public string Kontaktperson { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du må oppgi et telefonnummer.")]
    [Phone(ErrorMessage = "Telefonnummeret ser ikke gyldig ut.")]
    [Display(Name = "Telefon")]
    public string Telefon { get; set; } = string.Empty;

    // Fylles ut av det lille kartet i skjemaet (se wwwroot/js/posisjonsvelger.js).
    // Feltene er skjulte i skjemaet, så brukeren ser dem aldri direkte.
    // double? (med spørsmålstegn) betyr at verdien kan mangle. Da slår
    // [Required] til og brukeren får beskjed om å klikke i kartet.
    [Required(ErrorMessage = "Du må klikke i kartet for å markere plasseringen.")]
    public double? Breddegrad { get; set; }

    [Required(ErrorMessage = "Du må klikke i kartet for å markere plasseringen.")]
    public double? Lengdegrad { get; set; }
}
