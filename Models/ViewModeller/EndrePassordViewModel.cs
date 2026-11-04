using System.ComponentModel.DataAnnotations;

namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Skjemaet for å endre passord (Min profil → Endre passord).
/// Det nåværende passordet må alltid skrives inn, så ingen kan bytte passord
/// bare fordi de har fått tak i en innlogget PC.
/// </summary>
public class EndrePassordViewModel
{
    [Required(ErrorMessage = "Du må skrive inn nåværende passord.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nåværende passord")]
    public string NavarendePassord { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du må skrive inn et nytt passord.")]
    [DataType(DataType.Password)]
    // Samme lengde som i passordreglene til Identity (Program.cs). Resten av reglene
    // (tall, stor og liten bokstav) sjekkes av Identity når passordet lagres.
    [MinLength(8, ErrorMessage = "Passordet må være på minst 8 tegn.")]
    [Display(Name = "Nytt passord")]
    public string NyttPassord { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du må bekrefte det nye passordet.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NyttPassord), ErrorMessage = "Passordene er ikke like.")]
    [Display(Name = "Bekreft nytt passord")]
    public string BekreftNyttPassord { get; set; } = string.Empty;
}
