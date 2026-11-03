using Beredskapsportal.Data;
using Beredskapsportal.Models;
using Microsoft.EntityFrameworkCore;

namespace Beredskapsportal.Services;

/// <summary>
/// Lagrer ressurser i MariaDB.
/// </summary>
public class EfRessursRepository : IRessursRepository
{
    private readonly BeredskapsDbContext _db;

    public EfRessursRepository(BeredskapsDbContext db)
    {
        _db = db;
    }

    // Nyeste først (høyest id).
    public IReadOnlyList<Ressurs> HentAlle() =>
        _db.Ressurser.AsNoTracking().OrderByDescending(r => r.Id).ToList();

    // FirstOrDefault gir null hvis ingen ressurs har denne id-en.
    public Ressurs? HentMedId(int id) => _db.Ressurser.AsNoTracking().FirstOrDefault(r => r.Id == id);

    public Ressurs LeggTil(Ressurs ressurs)
    {
        _db.Ressurser.Add(ressurs);
        _db.SaveChanges();
        return ressurs;
    }

    public void SettStatus(int id, RessursStatus status, string? avvisningsgrunn = null)
    {
        // Find henter ressursen slik at EF følger med på endringer. SaveChanges lagrer endringen.
        var ressurs = _db.Ressurser.Find(id);
        if (ressurs is null)
        {
            return;
        }
        ressurs.Status = status;
        ressurs.Avvisningsgrunn = avvisningsgrunn;
        _db.SaveChanges();
    }

    public void Oppdater(Ressurs endret)
    {
        // Henter ressursen som EF følger med på, og kopierer over feltene som kan endres.
        // Eier (RegistrertAv) og Id kopieres bevisst ikke, de kan aldri endres.
        var ressurs = _db.Ressurser.Find(endret.Id);
        if (ressurs is null)
        {
            return;
        }
        KopierEndringer(endret, ressurs);
        _db.SaveChanges();
    }

    /// <summary>
    /// Feltene som kan endres på en ressurs. Samlet ett sted, så EF- og minnelagringen gjør det samme.
    /// </summary>
    internal static void KopierEndringer(Ressurs fra, Ressurs til)
    {
        til.Type = fra.Type;
        til.Status = fra.Status;
        til.Avvisningsgrunn = fra.Avvisningsgrunn;
        til.BeskrivelseAvKapasitet = fra.BeskrivelseAvKapasitet;
        til.Adresse = fra.Adresse;
        til.Tilgangsbeskrivelse = fra.Tilgangsbeskrivelse;
        til.Bilder = fra.Bilder.ToList();
        til.TilgjengeligFra = fra.TilgjengeligFra;
        til.TilgjengeligTil = fra.TilgjengeligTil;
        til.Kontaktperson = fra.Kontaktperson;
        til.Telefon = fra.Telefon;
        til.Breddegrad = fra.Breddegrad;
        til.Lengdegrad = fra.Lengdegrad;
    }

    public void Slett(int id)
    {
        var ressurs = _db.Ressurser.Find(id);
        if (ressurs is null)
        {
            return;
        }
        _db.Ressurser.Remove(ressurs);
        _db.SaveChanges();
    }

}