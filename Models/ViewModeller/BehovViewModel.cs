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
    [StringLength(2000, ErrorMessage = "Beskrivelsen kan være på maks 2000 tegn.")]
    public string Beskrivelse { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du må angi geografisk område.")]
    [Display(Name = "Geografisk område")]
    [StringLength(200, ErrorMessage = "Området kan være på maks 200 tegn.")]
    public string GeografiskOmrade { get; set; } = string.Empty;

    [Display(Name = "Prioritet")]
    public Prioritet Prioritet { get; set; } = Prioritet.Moderat;

    [Required(ErrorMessage = "Du må oppgi en kontaktperson.")]
    [Display(Name = "Kontaktperson")]
    [StringLength(200, ErrorMessage = "Kontaktpersonen kan være på maks 200 tegn.")]
    public string Kontaktperson { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du må oppgi et telefonnummer.")]
    [Phone(ErrorMessage = "Telefonnummeret ser ikke gyldig ut.")]
    [Display(Name = "Telefon")]
    [StringLength(30, ErrorMessage = "Telefonnummeret kan være på maks 30 tegn.")]
    public string Telefon { get; set; } = string.Empty;

    // Fylles ut av det lille kartet i skjemaet (se wwwroot/js/posisjonsvelger.js).
    // Feltene er skjulte i skjemaet, så brukeren ser dem aldri direkte.
    // double? (med spørsmålstegn) betyr at verdien kan mangle. Da slår
    // [Required] til og brukeren får beskjed om å klikke i kartet.
    //
    // Bare Breddegrad har [Required]. Kartet fyller alltid ut begge feltene
    // samtidig, så det holder å sjekke det ene. Hadde begge hatt [Required],
    // ville samme feilmelding blitt vist to ganger øverst i skjemaet.
    // Controlleren sjekker i tillegg at Lengdegrad ikke mangler.
    [Required(ErrorMessage = "Du må klikke i kartet for å markere plasseringen.")]
    public double? Breddegrad { get; set; }

    public double? Lengdegrad { get; set; }
}
