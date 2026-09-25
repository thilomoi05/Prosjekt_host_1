using Beredskapsportal.Models;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Controllers;

/// <summary>
/// Viser kartsiden og gir kartet dataene det trenger.
///
/// Slik henger det sammen:
///  1. Index() sender selve kartsiden (Views/Kart/Index.cshtml) til nettleseren.
///  2. JavaScript på siden (wwwroot/js/kart.js) spør deretter serveren om
///     punktene som skal vises, ved å kalle Behov() og Ressurser() under.
///  3. Svaret er JSON, en tekstformat JavaScript leser direkte.
///
/// Tilgang:
///  - Hele kartet krever innlogging ([Authorize] på klassen).
///  - Behov kan alle innloggede se.
///  - Ressurser kan bare Heimevernet og andre autoriserte se. Foreløpig er det
///    rollen "OffentligAktor". Sjekken skjer her på serveren, så det hjelper ikke
///    å skjule knapper i nettleseren for å komme rundt den.
/// </summary>
[Authorize]
public class KartController : Controller
{
    // Navnet på rollen som får se ressursene. Samlet ett sted, slik at det er
    // enkelt å endre når vi lager egne roller (f.eks. "Heimevernet").
    public const string RolleSomSerRessurser = nameof(BrukerRolle.OffentligAktor);

    private readonly IBehovRepository _behovRepository;
    private readonly IRessursRepository _ressursRepository;

    public KartController(IBehovRepository behovRepository, IRessursRepository ressursRepository)
    {
        _behovRepository = behovRepository;
        _ressursRepository = ressursRepository;
    }

    public IActionResult Index()
    {
        // Viewet trenger å vite om brukeren skal få se ressurser,
        // slik at det kan vise riktig tekst og tegnforklaring.
        ViewData["KanSeRessurser"] = User.IsInRole(RolleSomSerRessurser);
        return View();
    }

    /// <summary>
    /// Alle behov som en liste med JSON-objekter. Kalles av kart.js.
    /// Adresse: /Kart/Behov
    /// </summary>
    [HttpGet]
    public IActionResult Behov()
    {
        // Select() gjør hvert Behov-objekt om til et enklere objekt med bare
        // feltene kartet trenger.
        var punkter = _behovRepository.HentAlle().Select(behov => new
        {
            id = behov.Id,
            type = behov.VisningsNavn,
            beskrivelse = behov.Beskrivelse,
            omrade = behov.GeografiskOmrade,
            prioritet = behov.Prioritet.ToString(),   // "Akutt" eller "Planlagt"
            status = behov.Status.ToString(),
            breddegrad = behov.Breddegrad,
            lengdegrad = behov.Lengdegrad
        });

        // Json() gjør objektene om til JSON-tekst og sender dem til nettleseren.
        return Json(punkter);
    }

    /// <summary>
    /// Alle ressurser som en liste med JSON-objekter. Kalles av kart.js.
    /// Adresse: /Kart/Ressurser
    /// Bare brukere med riktig rolle slipper inn. Andre får feilkode 403 (ingen tilgang).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = RolleSomSerRessurser)]
    public IActionResult Ressurser()
    {
        var punkter = _ressursRepository.HentAlle().Select(ressurs => new
        {
            id = ressurs.Id,
            type = ressurs.Type.ToString(),
            beskrivelse = ressurs.BeskrivelseAvKapasitet,
            adresse = ressurs.Adresse,
            tilgang = ressurs.Tilgangsbeskrivelse,
            // Lager en adresse til hvert bilde, f.eks. "/Ressurs/Bilde/abc123.jpg".
            // Bildene sendes av RessursController.Bilde(), som også sjekker tilgang.
            bilder = ressurs.Bilder.Select(filnavn => Url.Action("Bilde", "Ressurs", new { id = filnavn })),
            kontaktperson = ressurs.Kontaktperson,
            telefon = ressurs.Telefon,
            breddegrad = ressurs.Breddegrad,
            lengdegrad = ressurs.Lengdegrad
        });

        return Json(punkter);
    }
}
