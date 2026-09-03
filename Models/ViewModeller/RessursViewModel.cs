using System.ComponentModel.DataAnnotations;

namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Data som sendes inn fra skjemaet "Registrer ressurs".
/// </summary>
public class RessursViewModel
{
    [Required(ErrorMessage = "Du må velge type ressurs.")]
    [Display(Name = "Type ressurs")]
    public RessursType? Type { get; set; }

    [Required(ErrorMessage = "Du må beskrive kapasiteten.")]
    [Display(Name = "Beskrivelse av kapasitet")]
    public string BeskrivelseAvKapasitet { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du må angi geografisk base/driftsområde.")]
    [Display(Name = "Geografisk base / driftsområde")]
    public string GeografiskBase { get; set; } = string.Empty;

    [Display(Name = "Tilgjengelig fra")]
    [DataType(DataType.Date)]
    public DateOnly? TilgjengeligFra { get; set; }

    [Display(Name = "Tilgjengelig til")]
    [DataType(DataType.Date)]
    public DateOnly? TilgjengeligTil { get; set; }

    [Required(ErrorMessage = "Du må oppgi en kontaktperson.")]
    [Display(Name = "Kontaktperson")]
    public string Kontaktperson { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du må oppgi et telefonnummer.")]
    [Phone(ErrorMessage = "Telefonnummeret ser ikke gyldig ut.")]
    [Display(Name = "Telefon")]
    public string Telefon { get; set; } = string.Empty;
}
