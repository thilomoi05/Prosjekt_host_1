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

    // Nyeste først, som i den minnebaserte versjonen.
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

    public int TellTilgjengelige() => _db.Ressurser.Count();
}