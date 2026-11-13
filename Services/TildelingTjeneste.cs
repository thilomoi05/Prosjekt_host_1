using Beredskapsportal.Models;

namespace Beredskapsportal.Services;

/// <summary>
/// Samler alt som endrer behov og ressurser etter at de er meldt inn:
///  - godkjenne og avvise ressurser (Admin)
///  - tildele ressurser til behov og fjerne tildelinger (Admin)
///  - forespørsler: offentlige aktører spør om en ressurs, Admin godkjenner eller avslår
///  - trekke tilbake behov (den som meldte det) og ressurser (eieren)
///
/// Hvorfor en egen klasse og ikke rett i controllerne? Flere sider gjør det samme
/// (f.eks. trekke tilbake en ressurs som er tildelt), og reglene må være like overalt:
///  - En ressurs kan bare være tildelt ett behov om gangen, og bare hvis den er godkjent.
///  - Et behov er Tildelt så lenge det har minst én ressurs, ellers Venter.
///  - Alt som skjer, skrives i historikken (Hendelse).
/// Controllerne sjekker HVEM som får gjøre noe. Denne klassen sørger for at det blir gjort riktig.
/// </summary>
public class TildelingTjeneste
{
    private readonly IBehovRepository _behovRepository;
    private readonly IRessursRepository _ressursRepository;
    private readonly ITildelingRepository _tildelingRepository;
    private readonly IHendelseRepository _hendelseRepository;
    private readonly IForesporselRepository _foresporselRepository;

    public TildelingTjeneste(IBehovRepository behovRepository, IRessursRepository ressursRepository,
        ITildelingRepository tildelingRepository, IHendelseRepository hendelseRepository,
        IForesporselRepository foresporselRepository)
    {
        _behovRepository = behovRepository;
        _ressursRepository = ressursRepository;
        _tildelingRepository = tildelingRepository;
        _hendelseRepository = hendelseRepository;
        _foresporselRepository = foresporselRepository;
    }

    // ---------------------------------------------------------------------
    // Historikk
    // ---------------------------------------------------------------------

    public IReadOnlyList<Hendelse> HistorikkForBehov(int behovId) => _hendelseRepository.HentForBehov(behovId);

    public IReadOnlyList<Hendelse> HistorikkForRessurs(int ressursId) => _hendelseRepository.HentForRessurs(ressursId);

    // De siste hendelsene i hele portalen, til adminloggen (AdminController.Logg).
    public IReadOnlyList<Hendelse> SisteHendelser(int antall) => _hendelseRepository.HentSiste(antall);

    /// <summary>Skriver én linje i historikken til et behov og/eller en ressurs.</summary>
    public void Logg(string beskrivelse, string utfortAv, int? behovId = null, int? ressursId = null)
    {
        _hendelseRepository.LeggTil(new Hendelse
        {
            Tidspunkt = DateTime.Now,
            UtfortAv = utfortAv,
            BehovId = behovId,
            RessursId = ressursId,
            Beskrivelse = beskrivelse
        });
    }

    // ---------------------------------------------------------------------
    // Godkjenning av ressurser
    // ---------------------------------------------------------------------

    public void Godkjenn(int ressursId, string utfortAv)
    {
        _ressursRepository.SettStatus(ressursId, RessursStatus.Godkjent);
        Logg("Ressursen ble godkjent", utfortAv, ressursId: ressursId);
    }

    public void Avvis(int ressursId, string begrunnelse, string utfortAv)
    {
        _ressursRepository.SettStatus(ressursId, RessursStatus.Avvist, begrunnelse);
        Logg($"Ressursen ble avvist: {begrunnelse}", utfortAv, ressursId: ressursId);
    }

    // ---------------------------------------------------------------------
    // Tildeling
    // ---------------------------------------------------------------------

    /// <summary>Ressurser som kan tildeles: godkjent og ikke tildelt noe behov.</summary>
    public List<Ressurs> LedigeRessurser()
    {
        var tildelteIder = _tildelingRepository.HentAlle().Select(t => t.RessursId).ToHashSet();
        return _ressursRepository.HentAlle()
            .Where(r => r.Status == RessursStatus.Godkjent && !tildelteIder.Contains(r.Id))
            .ToList();
    }

    /// <summary>Forslag til ressurser for et behov, nærmeste først (se Tildelingsforslag).</summary>
    public List<RessursForslag> LagForslag(Behov behov, bool visAlleTyper) =>
        Tildelingsforslag.Lag(behov, LedigeRessurser(), visAlleTyper);

    /// <summary>
    /// Tildeler en eller flere ressurser til et behov.
    /// Returnerer en feilmelding hvis noe ikke går, ellers null.
    /// </summary>
    public string? Tildel(int behovId, IReadOnlyCollection<int> ressursIder, string? kommentar, string utfortAv)
    {
        var behov = _behovRepository.HentMedId(behovId);
        if (behov is null)
        {
            return "Fant ikke behovet.";
        }
        if (ressursIder.Count == 0)
        {
            return "Velg minst én ressurs.";
        }

        // Sjekker alle ressursene før noe lagres, så vi ikke ender med halvveis tildeling.
        var ledigeIder = LedigeRessurser().Select(r => r.Id).ToHashSet();
        if (ressursIder.Any(id => !ledigeIder.Contains(id)))
        {
            return "En av ressursene er ikke ledig lenger. Last siden på nytt og prøv igjen.";
        }

        foreach (var ressursId in ressursIder)
        {
            var ressurs = _ressursRepository.HentMedId(ressursId)!;
            _tildelingRepository.LeggTil(new Tildeling
            {
                BehovId = behovId,
                RessursId = ressursId,
                Tidspunkt = DateTime.Now,
                TildeltAv = utfortAv,
                Kommentar = string.IsNullOrWhiteSpace(kommentar) ? null : kommentar.Trim()
            });

            Logg($"Tildelt {ressurs.VisningsNavn} ({ressurs.Adresse})", utfortAv, behovId: behovId);
            // Ressursens historikk sier ikke hvilket behov, fordi eieren (en privat aktør) også ser den.
            Logg("Ressursen ble tildelt et behov", utfortAv, ressursId: ressursId);

            // Ventende forespørsler om ressursen: den fra dette behovet er nå godkjent,
            // de fra andre behov lukkes, siden ressursen ikke er ledig lenger.
            foreach (var foresporsel in VentendeForesporsler(ressursId: ressursId))
            {
                if (foresporsel.BehovId == behovId)
                {
                    _foresporselRepository.SettStatus(foresporsel.Id, ForesporselStatus.Godkjent, null, utfortAv);
                }
                else
                {
                    LukkForesporsel(foresporsel, "Ressursen ble tildelt et annet behov.", utfortAv);
                }
            }
        }

        _behovRepository.SettStatus(behovId, BehovStatus.Tildelt);
        return null;
    }

    /// <summary>Fjerner én tildeling. Ressursen blir ledig og kan tildeles et annet behov.</summary>
    public void FjernTildeling(int behovId, int ressursId, string utfortAv)
    {
        var ressurs = _ressursRepository.HentMedId(ressursId);
        _tildelingRepository.Fjern(behovId, ressursId);

        Logg($"Tildelingen av {ressurs?.VisningsNavn ?? "en ressurs"} ble fjernet", utfortAv, behovId: behovId);
        Logg("Tildelingen ble fjernet. Ressursen er ledig igjen", utfortAv, ressursId: ressursId);

        OppdaterBehovStatus(behovId);
    }

    // ---------------------------------------------------------------------
    // Forespørsler
    // ---------------------------------------------------------------------

    /// <summary>
    /// En offentlig aktør spør om å få en ressurs til sitt behov. Controlleren har allerede
    /// sjekket at behovet er brukerens eget. Returnerer en feilmelding, eller null hvis det gikk.
    /// </summary>
    public string? Foresporr(int behovId, int ressursId, string? kommentar, string forespurtAv)
    {
        if (_behovRepository.HentMedId(behovId) is null)
        {
            return "Fant ikke behovet.";
        }
        var ressurs = LedigeRessurser().FirstOrDefault(r => r.Id == ressursId);
        if (ressurs is null)
        {
            return "Ressursen er ikke ledig lenger.";
        }
        if (VentendeForesporsler(behovId: behovId, ressursId: ressursId).Count > 0)
        {
            return "Du har allerede spurt om denne ressursen til dette behovet.";
        }

        _foresporselRepository.LeggTil(new Foresporsel
        {
            BehovId = behovId,
            RessursId = ressursId,
            ForespurtAv = forespurtAv,
            Tidspunkt = DateTime.Now,
            Kommentar = string.IsNullOrWhiteSpace(kommentar) ? null : kommentar.Trim()
        });

        Logg($"Forespurte {ressurs.VisningsNavn} ({ressurs.Adresse})", forespurtAv, behovId: behovId);
        return null;
    }

    /// <summary>
    /// Admin godkjenner en forespørsel: ressursen tildeles behovet (Tildel over), og andre
    /// forespørsler om samme ressurs lukkes. Returnerer en feilmelding, eller null hvis det gikk.
    /// </summary>
    public string? GodkjennForesporsel(int foresporselId, string utfortAv)
    {
        var foresporsel = _foresporselRepository.HentMedId(foresporselId);
        if (foresporsel is null || foresporsel.Status != ForesporselStatus.Venter)
        {
            return "Forespørselen er allerede behandlet.";
        }

        // Tildel setter også denne forespørselen til Godkjent.
        return Tildel(foresporsel.BehovId, new[] { foresporsel.RessursId }, foresporsel.Kommentar, utfortAv);
    }

    /// <summary>Alle forespørsler til ett behov, nyeste først (vises på detaljsiden til behovet).</summary>
    public List<Foresporsel> ForesporslerForBehov(int behovId) =>
        _foresporselRepository.HentAlle().Where(f => f.BehovId == behovId).ToList();

    /// <summary>Alle forespørsler som venter på en administrator, eldste først (adminsiden).</summary>
    public List<Foresporsel> AlleVentendeForesporsler() =>
        VentendeForesporsler().OrderBy(f => f.Tidspunkt).ToList();

    /// <summary>Admin avslår en forespørsel, med en begrunnelse den som spurte ser.</summary>
    public void AvslaForesporsel(int foresporselId, string begrunnelse, string utfortAv)
    {
        var foresporsel = _foresporselRepository.HentMedId(foresporselId);
        if (foresporsel is null || foresporsel.Status != ForesporselStatus.Venter)
        {
            return;
        }
        LukkForesporsel(foresporsel, begrunnelse, utfortAv);
    }

    // ---------------------------------------------------------------------
    // Trekke tilbake
    // ---------------------------------------------------------------------

    /// <summary>
    /// Trekker tilbake (sletter) et behov. Ressursene som var tildelt behovet, blir ledige igjen.
    /// </summary>
    public void TrekkTilbakeBehov(int behovId, string utfortAv)
    {
        foreach (var tildeling in TildelingerFor(behovId: behovId))
        {
            _tildelingRepository.Fjern(behovId, tildeling.RessursId);
            Logg("Behovet ressursen var tildelt, ble trukket tilbake. Ressursen er ledig igjen", utfortAv, ressursId: tildeling.RessursId);
        }

        foreach (var foresporsel in VentendeForesporsler(behovId: behovId))
        {
            LukkForesporsel(foresporsel, "Behovet ble trukket tilbake.", utfortAv);
        }

        Logg("Behovet ble trukket tilbake", utfortAv, behovId: behovId);
        _behovRepository.Slett(behovId);
    }

    /// <summary>
    /// Trekker tilbake (sletter) en ressurs. Var den tildelt et behov, fjernes tildelingen,
    /// og behovet går tilbake til Venter hvis det ikke har andre ressurser.
    /// </summary>
    public void TrekkTilbakeRessurs(int ressursId, string utfortAv)
    {
        var ressurs = _ressursRepository.HentMedId(ressursId);

        foreach (var tildeling in TildelingerFor(ressursId: ressursId))
        {
            _tildelingRepository.Fjern(tildeling.BehovId, ressursId);
            Logg($"{ressurs?.VisningsNavn ?? "En ressurs"} ble trukket tilbake av eieren", utfortAv, behovId: tildeling.BehovId);
            OppdaterBehovStatus(tildeling.BehovId);
        }

        foreach (var foresporsel in VentendeForesporsler(ressursId: ressursId))
        {
            LukkForesporsel(foresporsel, "Ressursen ble trukket tilbake av eieren.", utfortAv);
        }

        Logg("Ressursen ble trukket tilbake", utfortAv, ressursId: ressursId);
        _ressursRepository.Slett(ressursId);
    }

    // ---------------------------------------------------------------------
    // Hjelpere
    // ---------------------------------------------------------------------

    private List<Tildeling> TildelingerFor(int? behovId = null, int? ressursId = null) =>
        _tildelingRepository.HentAlle()
            .Where(t => (behovId is null || t.BehovId == behovId) && (ressursId is null || t.RessursId == ressursId))
            .ToList();

    private List<Foresporsel> VentendeForesporsler(int? behovId = null, int? ressursId = null) =>
        _foresporselRepository.HentAlle()
            .Where(f => f.Status == ForesporselStatus.Venter
                && (behovId is null || f.BehovId == behovId) && (ressursId is null || f.RessursId == ressursId))
            .ToList();

    /// <summary>Setter en forespørsel til Avslått med en begrunnelse, og skriver det i historikken til behovet.</summary>
    private void LukkForesporsel(Foresporsel foresporsel, string begrunnelse, string utfortAv)
    {
        _foresporselRepository.SettStatus(foresporsel.Id, ForesporselStatus.Avslatt, begrunnelse, utfortAv);
        Logg($"Forespørsel avslått: {begrunnelse}", utfortAv, behovId: foresporsel.BehovId);
    }

    /// <summary>Et behov er Tildelt hvis det har minst én ressurs, ellers Venter.</summary>
    private void OppdaterBehovStatus(int behovId)
    {
        var harRessurser = TildelingerFor(behovId: behovId).Count > 0;
        _behovRepository.SettStatus(behovId, harRessurser ? BehovStatus.Tildelt : BehovStatus.Venter);
    }
}
