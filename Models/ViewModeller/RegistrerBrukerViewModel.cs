using System.ComponentModel.DataAnnotations;

namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Data som sendes inn fra registreringsskjemaet for nye brukere.
/// </summary>
public class RegistrerBrukerViewModel
{
    [Required(ErrorMessage = "Du må skrive inn fullt navn.")]
    [Display(Name = "Fullt navn")]
    public string FulltNavn { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du må skrive inn e-post.")]
    [EmailAddress(ErrorMessage = "E-posten ser ikke gyldig ut.")]
    [Display(Name = "E-post")]
    public string Epost { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du må velge et brukernavn.")]
    [Display(Name = "Brukernavn")]
    public string Brukernavn { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du må lage et passord.")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Passordet må være på minst 6 tegn.")]
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
