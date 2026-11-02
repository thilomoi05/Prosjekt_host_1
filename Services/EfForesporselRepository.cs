using Beredskapsportal.Data;
using Beredskapsportal.Models;
using Microsoft.EntityFrameworkCore;

namespace Beredskapsportal.Services;

/// <summary>
/// Lagrer forespørsler om ressurser i MariaDB.
/// </summary>
public class EfForesporselRepository : IForesporselRepository
{
    private readonly BeredskapsDbContext _db;

    public EfForesporselRepository(BeredskapsDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<Foresporsel> HentAlle() =>
        _db.Foresporsler.AsNoTracking().OrderByDescending(f => f.Tidspunkt).ThenByDescending(f => f.Id).ToList();

    public Foresporsel? HentMedId(int id) => _db.Foresporsler.AsNoTracking().FirstOrDefault(f => f.Id == id);

    public Foresporsel LeggTil(Foresporsel foresporsel)
    {
        _db.Foresporsler.Add(foresporsel);
        _db.SaveChanges();
        return foresporsel;
    }

    public void SettStatus(int id, ForesporselStatus status, string? begrunnelse, string behandletAv)
    {
        // Find henter forespørselen slik at EF følger med på endringer. SaveChanges lagrer endringen.
        var foresporsel = _db.Foresporsler.Find(id);
        if (foresporsel is null)
        {
            return;
        }
        foresporsel.Status = status;
        foresporsel.Begrunnelse = begrunnelse;
        foresporsel.BehandletAv = behandletAv;
        _db.SaveChanges();
    }
}
