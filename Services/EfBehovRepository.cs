using Beredskapsportal.Data;
using Beredskapsportal.Models;
using Microsoft.EntityFrameworkCore;

namespace Beredskapsportal.Services;

/// <summary>
/// Lagrer behov i MariaDB. Samme interface som InMemoryBehovRepository,
/// så Controller-ene merker ingen forskjell.
/// </summary>
public class EfBehovRepository : IBehovRepository
{
    private readonly BeredskapsDbContext _db;

    public EfBehovRepository(BeredskapsDbContext db)
    {
        _db = db;
    }

    // Nyeste først (høyest dato, deretter høyest id).
    private IQueryable<Behov> NyesteForst() =>
        _db.Behov.AsNoTracking().OrderByDescending(b => b.Dato).ThenByDescending(b => b.Id);

    public IReadOnlyList<Behov> HentAlle() => NyesteForst().ToList();

    // FirstOrDefault gir null hvis ingen behov har denne id-en.
    public Behov? HentMedId(int id) => _db.Behov.AsNoTracking().FirstOrDefault(b => b.Id == id);

    public IReadOnlyList<Behov> HentSiste(int antall) => NyesteForst().Take(antall).ToList();

    public Behov LeggTil(Behov behov)
    {
        _db.Behov.Add(behov);
        _db.SaveChanges();
        return behov;
    }

    public void SettStatus(int id, BehovStatus status)
    {
        // Find henter behovet slik at EF følger med på endringer. SaveChanges lagrer endringen.
        var behov = _db.Behov.Find(id);
        if (behov is null)
        {
            return;
        }
        behov.Status = status;
        _db.SaveChanges();
    }

    public void Oppdater(Behov endret)
    {
        var behov = _db.Behov.Find(endret.Id);
        if (behov is null)
        {
            return;
        }
        KopierEndringer(endret, behov);
        _db.SaveChanges();
    }

    /// <summary>
    /// Feltene som kan endres på et behov. Samlet ett sted, så EF- og minnelagringen gjør det samme.
    /// Status (styres av tildelingene), Dato (når det ble meldt) og RegistrertAv (hvem som meldte det)
    /// kopieres bevisst ikke.
    /// </summary>
    internal static void KopierEndringer(Behov fra, Behov til)
    {
        til.Type = fra.Type;
        til.Beskrivelse = fra.Beskrivelse;
        til.GeografiskOmrade = fra.GeografiskOmrade;
        til.Prioritet = fra.Prioritet;
        til.Kontaktperson = fra.Kontaktperson;
        til.Telefon = fra.Telefon;
        til.Breddegrad = fra.Breddegrad;
        til.Lengdegrad = fra.Lengdegrad;
    }

    public void Slett(int id)
    {
        var behov = _db.Behov.Find(id);
        if (behov is null)
        {
            return;
        }
        _db.Behov.Remove(behov);
        _db.SaveChanges();
    }

    // Behov som venter på en ressurs. Tildelte behov regnes som løst.
    public int TellAktive() => _db.Behov.Count(b => b.Status == BehovStatus.Venter);

    // Ventende behov med en gitt prioritet, f.eks. TellVentende(Prioritet.Hoy).
    public int TellVentende(Prioritet prioritet) =>
        _db.Behov.Count(b => b.Prioritet == prioritet && b.Status == BehovStatus.Venter);

    // Behov som har fått minst én ressurs.
    public int TellTildelte() => _db.Behov.Count(b => b.Status == BehovStatus.Tildelt);
}
