using Beredskapsportal.Models;

namespace Beredskapsportal.Services;

/// <summary>
/// Midlertidig, minnebasert lagring av behov. Forhåndsfylt med noen
/// eksempeldata slik at dashbordet ("Oversikt") viser noe fornuftig helt fra start.
/// Registrert som Singleton i Program.cs - se InMemoryBrukerRepository for
/// begrunnelsen bak dette valget.
/// </summary>
public class InMemoryBehovRepository : IBehovRepository
{
    // Listen holdes sortert med det nyeste behovet først (index 0), slik at
    // "HentSiste" alltid kan ta de første elementene uten å sortere på nytt.
    private readonly List<Behov> _behov;
    private readonly object _lasObjekt = new();
    private int _nesteId;

    public InMemoryBehovRepository()
    {
        _behov = LagEksempelData();
        _nesteId = _behov.Count + 1;
    }

    public IReadOnlyList<Behov> HentAlle()
    {
        lock (_lasObjekt)
        {
            return _behov.ToList();
        }
    }

    public IReadOnlyList<Behov> HentSiste(int antall)
    {
        lock (_lasObjekt)
        {
            return _behov.Take(antall).ToList();
        }
    }

    public Behov LeggTil(Behov behov)
    {
        lock (_lasObjekt)
        {
            behov.Id = _nesteId++;
            // Settes inn først i listen slik at den vises øverst under "Siste meldte behov".
            _behov.Insert(0, behov);
            return behov;
        }
    }

    /// <summary>
    /// Behov som venter på en ressurs. Tildelte behov regnes som løst og telles ikke.
    /// </summary>
    public int TellAktive()
    {
        lock (_lasObjekt)
        {
            return _behov.Count(b => b.Status == BehovStatus.Venter);
        }
    }

    /// <summary>
    /// Behov med den gitte prioriteten som fortsatt venter på en ressurs.
    /// Eksempel: TellVentende(Prioritet.Hoy) gir antall høyt prioriterte behov som venter.
    /// </summary>
    public int TellVentende(Prioritet prioritet)
    {
        lock (_lasObjekt)
        {
            return _behov.Count(b => b.Prioritet == prioritet && b.Status == BehovStatus.Venter);
        }
    }

    /// <summary>
    /// Behov som har fått minst én ressurs.
    /// </summary>
    public int TellTildelte()
    {
        lock (_lasObjekt)
        {
            return _behov.Count(b => b.Status == BehovStatus.Tildelt);
        }
    }

    // Behov 3, 6, 8 og 11 har status Tildelt, fordi de har tildelte ressurser
    // i eksempeldataene i InMemoryTildelingRepository. Resten venter.
    internal static List<Behov> LagEksempelData()
    {
        var data = new List<Behov>
        {
            new() { Id = 1, Type = BehovType.Nodstrom, Beskrivelse = "Behov for nødaggregat til lokalt bosenter.", GeografiskOmrade = "Lund", Prioritet = Prioritet.Hoy, Status = BehovStatus.Venter, Kontaktperson = "Kommunalt beredskapskontor", Telefon = "38070000", Dato = new DateOnly(2026, 9, 2) },
            new() { Id = 2, Type = BehovType.Drivstofftransport, Beskrivelse = "Transport av drivstoff til aggregater i bydelen.", GeografiskOmrade = "Vågsbygd", Prioritet = Prioritet.Hoy, Status = BehovStatus.Venter, Kontaktperson = "Kommunalt beredskapskontor", Telefon = "38070001", Dato = new DateOnly(2026, 9, 2) },
            new() { Id = 3, Type = BehovType.Kommunikasjonsutstyr, Beskrivelse = "Reservesamband til lokalt legevaktsentral.", GeografiskOmrade = "Grim", Prioritet = Prioritet.Hoy, Status = BehovStatus.Tildelt, Kontaktperson = "Sørlandet sykehus", Telefon = "38070002", Dato = new DateOnly(2026, 9, 1) },
            new() { Id = 4, Type = BehovType.Nodbelysning, Beskrivelse = "Nødbelysning i evakueringsrute.", GeografiskOmrade = "Eg", Prioritet = Prioritet.Lav, Status = BehovStatus.Venter, Kontaktperson = "Brann og redning", Telefon = "38070003", Dato = new DateOnly(2026, 9, 1) },
            new() { Id = 5, Type = BehovType.Oppvarming, Beskrivelse = "Varmeovner til midlertidig varmestue.", GeografiskOmrade = "Sentrum", Prioritet = Prioritet.Hoy, Status = BehovStatus.Venter, Kontaktperson = "Kommunalt beredskapskontor", Telefon = "38070004", Dato = new DateOnly(2026, 9, 2) },
            new() { Id = 6, Type = BehovType.Nodstrom, Beskrivelse = "Aggregat til vannbehandlingsanlegg.", GeografiskOmrade = "Vågsbygd", Prioritet = Prioritet.Moderat, Status = BehovStatus.Tildelt, Kontaktperson = "Kommunalteknisk etat", Telefon = "38070005", Dato = new DateOnly(2026, 8, 31) },
            new() { Id = 7, Type = BehovType.Kommunikasjonsutstyr, Beskrivelse = "Satellittelefoner til utrykningsenheter.", GeografiskOmrade = "Randesund", Prioritet = Prioritet.Hoy, Status = BehovStatus.Venter, Kontaktperson = "Politiet", Telefon = "38070006", Dato = new DateOnly(2026, 8, 31) },
            new() { Id = 8, Type = BehovType.Drivstofftransport, Beskrivelse = "Diesel til reservekraft ved sykehjem.", GeografiskOmrade = "Sentrum", Prioritet = Prioritet.Moderat, Status = BehovStatus.Tildelt, Kontaktperson = "Kommunalt beredskapskontor", Telefon = "38070007", Dato = new DateOnly(2026, 8, 31) },
            new() { Id = 9, Type = BehovType.Oppvarming, Beskrivelse = "Varmeovner til akuttmottak.", GeografiskOmrade = "Lund", Prioritet = Prioritet.Hoy, Status = BehovStatus.Venter, Kontaktperson = "Sørlandet sykehus", Telefon = "38070008", Dato = new DateOnly(2026, 8, 30) },
            new() { Id = 10, Type = BehovType.Nodbelysning, Beskrivelse = "Nødlys i parkeringshus.", GeografiskOmrade = "Grim", Prioritet = Prioritet.Lav, Status = BehovStatus.Venter, Kontaktperson = "Kommunalt beredskapskontor", Telefon = "38070009", Dato = new DateOnly(2026, 8, 30) },
            new() { Id = 11, Type = BehovType.Nodstrom, Beskrivelse = "Aggregat til brannstasjon.", GeografiskOmrade = "Eg", Prioritet = Prioritet.Hoy, Status = BehovStatus.Tildelt, Kontaktperson = "Brann og redning", Telefon = "38070010", Dato = new DateOnly(2026, 8, 29) },
            new() { Id = 12, Type = BehovType.Drivstofftransport, Beskrivelse = "Bensin til utrykningskjøretøy.", GeografiskOmrade = "Vågsbygd", Prioritet = Prioritet.Moderat, Status = BehovStatus.Venter, Kontaktperson = "Politiet", Telefon = "38070011", Dato = new DateOnly(2026, 8, 29) },
        };

        // Gir hvert eksempelbehov en plassering på kartet ut fra området det gjelder.
        foreach (var behov in data)
        {
            // Behov har ingen adresse, bare et område, så adresseteksten (_) brukes ikke her.
            (_, behov.Breddegrad, behov.Lengdegrad) = EksempelPosisjoner.For(behov.GeografiskOmrade);
        }

        return data;
    }
}
