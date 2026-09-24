using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Controllers;

/// <summary>
/// Håndterer registrering av ressurser som bedrifter, entreprenører og privatpersoner tilbyr,
/// inkludert opplasting og visning av bilder av ressursen.
/// </summary>
[Authorize]
public class RessursController : Controller
{
    // Regler for bildeopplasting. Samlet her, så de er lette å finne og endre.
    private const int MaksAntallBilder = 5;
    private const long MaksBildestorrelse = 5 * 1024 * 1024;   // 5 MB per bilde
    private static readonly string[] TillatteFiltyper = { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly IRessursRepository _ressursRepository;

    // Mappen der bildene lagres. Den ligger bevisst UTENFOR wwwroot:
    // alt i wwwroot kan alle laste ned, men bildene skal bare autoriserte se.
    private readonly string _bildemappe;

    // IWebHostEnvironment gir oss blant annet stien til prosjektmappen (ContentRootPath).
    public RessursController(IRessursRepository ressursRepository, IWebHostEnvironment miljo)
    {
        _ressursRepository = ressursRepository;
        _bildemappe = Path.Combine(miljo.ContentRootPath, "Opplastinger", "bilder");
    }

    // Nav-punktet "Ressurser" peker rett på registreringsskjemaet,
    // siden det foreløpig ikke finnes noen egen liste-visning for ressurser.
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Registrer));
    }

    [HttpGet]
    public IActionResult Registrer()
    {
        return View(new RessursViewModel());
    }

    // "async Task" fordi det tar litt tid å lagre filer på disk. Mens vi venter
    // (await), kan serveren gjøre andre ting i stedet for å stå og vente.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrer(RessursViewModel modell)
    {
        // Sjekker bildene i tillegg til de vanlige [Required]-reglene i view-modellen.
        SjekkBilder(modell.Bilder);

        if (!ModelState.IsValid)
        {
            return View(modell);
        }

        // Lagrer bildene på disk og tar vare på filnavnene.
        var filnavn = new List<string>();
        foreach (var bilde in modell.Bilder)
        {
            filnavn.Add(await LagreBilde(bilde));
        }

        var nyRessurs = new Ressurs
        {
            Type = modell.Type!.Value,
            BeskrivelseAvKapasitet = modell.BeskrivelseAvKapasitet,
            Adresse = modell.Adresse,
            Tilgangsbeskrivelse = modell.Tilgangsbeskrivelse,
            Bilder = filnavn,
            TilgjengeligFra = modell.TilgjengeligFra,
            TilgjengeligTil = modell.TilgjengeligTil,
            Kontaktperson = modell.Kontaktperson,
            Telefon = modell.Telefon,
            // Trygt å bruke ".Value": ModelState.IsValid har sjekket at feltene finnes.
            Breddegrad = modell.Breddegrad!.Value,
            Lengdegrad = modell.Lengdegrad!.Value
        };

        _ressursRepository.LeggTil(nyRessurs);

        return RedirectToAction("Index", "Oversikt");
    }

    /// <summary>
    /// Sender ett bilde til nettleseren, f.eks. /Ressurs/Bilde/3f2a...jpg.
    /// Bare Heimevernet og andre autoriserte får se bildene (samme regel som på kartet).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = KartController.RolleSomSerRessurser)]
    public IActionResult Bilde(string id)
    {
        // Path.GetFileName fjerner eventuelle mappenavn fra id-en. Da kan ingen
        // lure oss til å sende andre filer, f.eks. med "../../Program.cs".
        var filnavn = Path.GetFileName(id);
        var sti = Path.Combine(_bildemappe, filnavn);

        if (!System.IO.File.Exists(sti))
        {
            return NotFound();
        }

        // Finner riktig filtype, så nettleseren vet at det er et bilde.
        var filtype = Path.GetExtension(filnavn).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        return PhysicalFile(sti, filtype);
    }

    /// <summary>
    /// Sjekker at brukeren har lastet opp 1-5 bilder, at de er bildefiler,
    /// og at ingen er for store. Feil legges i ModelState og vises i skjemaet.
    /// </summary>
    private void SjekkBilder(List<IFormFile> bilder)
    {
        if (bilder.Count == 0)
        {
            ModelState.AddModelError(nameof(RessursViewModel.Bilder), "Du må legge ved minst ett bilde av ressursen.");
            return;
        }

        if (bilder.Count > MaksAntallBilder)
        {
            ModelState.AddModelError(nameof(RessursViewModel.Bilder), $"Du kan legge ved maks {MaksAntallBilder} bilder.");
        }

        foreach (var bilde in bilder)
        {
            var filtype = Path.GetExtension(bilde.FileName).ToLowerInvariant();

            if (!TillatteFiltyper.Contains(filtype))
            {
                ModelState.AddModelError(nameof(RessursViewModel.Bilder), $"«{bilde.FileName}» er ikke et bilde. Bruk JPG, PNG eller WEBP.");
            }

            if (bilde.Length > MaksBildestorrelse)
            {
                ModelState.AddModelError(nameof(RessursViewModel.Bilder), $"«{bilde.FileName}» er større enn 5 MB.");
            }
        }
    }

    /// <summary>
    /// Lagrer ett bilde i bildemappen og returnerer filnavnet det fikk.
    /// </summary>
    private async Task<string> LagreBilde(IFormFile bilde)
    {
        // Lager mappen hvis den ikke finnes (gjør ingenting hvis den finnes fra før).
        Directory.CreateDirectory(_bildemappe);

        // Gir filen et nytt, tilfeldig navn (Guid). Da kan to brukere laste opp
        // "bilde.jpg" uten å overskrive hverandre, og ingen kan gjette filnavnet.
        var filnavn = Guid.NewGuid() + Path.GetExtension(bilde.FileName).ToLowerInvariant();
        var sti = Path.Combine(_bildemappe, filnavn);

        // "using" sørger for at filen lukkes når vi er ferdige med å skrive til den.
        using var fil = new FileStream(sti, FileMode.Create);
        await bilde.CopyToAsync(fil);

        return filnavn;
    }
}
