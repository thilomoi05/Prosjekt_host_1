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
}
