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
    [StringLength(2000, ErrorMessage = "Beskrivelsen kan være på maks 2000 tegn.")]
    public string BeskrivelseAvKapasitet { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du må skrive inn adressen der ressursen er.")]
    [Display(Name = "Adresse")]
    [StringLength(300, ErrorMessage = "Adressen kan være på maks 300 tegn.")]
    public string Adresse { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du må beskrive hvordan man får tak i ressursen.")]
    [Display(Name = "Hvordan får man tak i ressursen?")]
    [StringLength(1000, ErrorMessage = "Beskrivelsen av tilgang kan være på maks 1000 tegn.")]
    public string Tilgangsbeskrivelse { get; set; } = string.Empty;

    // IFormFile er ASP.NET Core sin type for en opplastet fil.
    // En liste, fordi brukeren kan laste opp flere bilder samtidig.
    // Sjekken av bildene (antall, type og størrelse) skjer i RessursController.
    [Display(Name = "Bilder av ressursen")]
    public List<IFormFile> Bilder { get; set; } = new();

    [Display(Name = "Tilgjengelig fra")]
    [DataType(DataType.Date)]
    public DateOnly? TilgjengeligFra { get; set; }

    [Display(Name = "Tilgjengelig til")]
    [DataType(DataType.Date)]
    public DateOnly? TilgjengeligTil { get; set; }

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
