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