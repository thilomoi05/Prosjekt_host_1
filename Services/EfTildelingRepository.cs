using Beredskapsportal.Data;
using Beredskapsportal.Models;
using Microsoft.EntityFrameworkCore;

namespace Beredskapsportal.Services;

/// <summary>
/// Lagrer tildelinger (hvilke ressurser som er satt inn på hvilke behov) i MariaDB.
/// </summary>
public class EfTildelingRepository : ITildelingRepository
{
    private readonly BeredskapsDbContext _db;

    public EfTildelingRepository(BeredskapsDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<Tildeling> HentAlle() =>
        _db.Tildelinger.AsNoTracking().OrderByDescending(t => t.Tidspunkt).ToList();

    public Tildeling LeggTil(Tildeling tildeling)
    {
        _db.Tildelinger.Add(tildeling);
        _db.SaveChanges();
        return tildeling;
    }

    public void Fjern(int behovId, int ressursId)
    {
        // Nøkkelen er (BehovId, RessursId), i den rekkefølgen (se BeredskapsDbContext).
        var tildeling = _db.Tildelinger.Find(behovId, ressursId);
        if (tildeling is null)
        {
            return;
        }
        _db.Tildelinger.Remove(tildeling);
        _db.SaveChanges();
    }
}