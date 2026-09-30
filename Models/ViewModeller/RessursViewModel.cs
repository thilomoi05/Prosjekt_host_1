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

    [Required(ErrorMessage = "Du må skrive inn adressen der ressursen er.")]
    [Display(Name = "Adresse")]
    public string Adresse { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du må beskrive hvordan man får tak i ressursen.")]
    [Display(Name = "Hvordan får man tak i ressursen?")]
    public string Tilgangsbeskrivelse { get; set; } = string.Empty;

    // IFormFile er ASP.NET Core sin type for en opplastet fil.
    // En liste, fordi brukeren kan laste opp flere bilder samtidig.
    // Sjekken av bildene (antall, type og størrelse) skjer i RessursController.
    [Display(Name = "Bilder av ressursen")]
    public List<IFormFile> Bilder { get; set; } = new();

    // Avkrysningsboksen "Plasseringen på kartet er riktig".
    // [Range] med true/true betyr at boksen MÅ være krysset av.
    [Range(typeof(bool), "true", "true", ErrorMessage = "Du må bekrefte at plasseringen på kartet er riktig.")]
    [Display(Name = "Plasseringen på kartet er riktig")]
    public bool PlasseringBekreftet { get; set; }

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
