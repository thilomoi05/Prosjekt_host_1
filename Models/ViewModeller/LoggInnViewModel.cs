using System.ComponentModel.DataAnnotations;

namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Data som sendes inn fra innloggingsskjemaet.
/// Et eget "ViewModel" holder skjemafeltene atskilt fra selve domenemodellen (Bruker),
/// slik at vi ikke risikerer å binde felter (som Rolle) direkte fra brukerinput.
/// </summary>
public class LoggInnViewModel
{
    [Required(ErrorMessage = "Du må skrive inn brukernavn.")]
    [Display(Name = "Brukernavn")]
    public string Brukernavn { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du må skrive inn passord.")]
    [DataType(DataType.Password)]
    [Display(Name = "Passord")]
    public string Passord { get; set; } = string.Empty;

    // Siden brukeren prøvde å åpne før innlogging, f.eks. "/Kart". Ligger i et skjult felt i skjemaet.
    public string? ReturnUrl { get; set; }
}
