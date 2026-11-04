using System.ComponentModel.DataAnnotations;

namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Skjemaet for å endre navn og e-post (Min profil → Endre navn og e-post).
///
/// Brukernavnet er ikke med, fordi det ikke kan endres: behov, ressurser og forespørsler
/// er koblet til eieren med brukernavnet (RegistrertAv, ForespurtAv).
/// Samme lengder og feilmeldinger som ved registrering (RegistrerBrukerViewModel).
/// </summary>
public class EndreProfilViewModel
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

    // Trengs bare når e-posten endres. Det sjekkes i KontoController.EndreProfil,
    // fordi [Required] ikke kan si "bare hvis et annet felt er endret".
    [DataType(DataType.Password)]
    [Display(Name = "Nåværende passord")]
    public string? NavarendePassord { get; set; }
}
