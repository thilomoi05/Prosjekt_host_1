using System.ComponentModel.DataAnnotations;

namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Data som sendes inn fra innloggingsskjemaet.
/// Et eget "ViewModel" holder skjemafeltene atskilt fra selve domenemodellen (Bruker),
/// slik at vi ikke risikerer å binde felter (som PassordHash) direkte fra brukerinput.
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
}
