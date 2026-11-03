using System.ComponentModel.DataAnnotations;

namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Data som sendes inn fra registreringsskjemaet for nye brukere.
/// </summary>
public class RegistrerBrukerViewModel
{
    [Required(ErrorMessage = "Du må skrive inn fullt navn.")]
    [Display(Name = "Fullt navn")]
    [StringLength(200, ErrorMessage = "Navnet kan være på maks 200 tegn.")]
    public string FulltNavn { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du må skrive inn e-post.")]
    [EmailAddress(ErrorMessage = "E-posten ser ikke gyldig ut.")]
    [Display(Name = "E-post")]
    [StringLength(256, ErrorMessage = "E-posten kan være på maks 256 tegn.")]
    public string Epost { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du må velge et brukernavn.")]
    [Display(Name = "Brukernavn")]
    [StringLength(100, ErrorMessage = "Brukernavnet kan være på maks 100 tegn.")]
    public string Brukernavn { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du må lage et passord.")]
    [DataType(DataType.Password)]
    // Samme lengde som i passordreglene til Identity (Program.cs). Resten av reglene
    // (tall, stor og liten bokstav) sjekkes av Identity når brukeren lagres.
    [MinLength(8, ErrorMessage = "Passordet må være på minst 8 tegn.")]
    [Display(Name = "Passord")]
    public string Passord { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du må bekrefte passordet.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Passord), ErrorMessage = "Passordene er ikke like.")]
    [Display(Name = "Bekreft passord")]
    public string BekreftPassord { get; set; } = string.Empty;

    [Display(Name = "Rolle")]
    public BrukerRolle Rolle { get; set; } = BrukerRolle.OffentligAktor;
}
