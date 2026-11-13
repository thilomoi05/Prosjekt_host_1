using Beredskapsportal.Data;
using Beredskapsportal.Models;
using Microsoft.EntityFrameworkCore;

namespace Beredskapsportal.Services;

/// <summary>
/// Lagrer historikken (Hendelse) i MariaDB.
/// </summary>
public class EfHendelseRepository : IHendelseRepository
{
    private readonly BeredskapsDbContext _db;

    public EfHendelseRepository(BeredskapsDbContext db)
    {
        _db = db;
    }

    public void LeggTil(Hendelse hendelse)
    {
        _db.Hendelser.Add(hendelse);
        _db.SaveChanges();
    }

    public IReadOnlyList<Hendelse> HentForBehov(int behovId) =>
        _db.Hendelser.AsNoTracking().Where(h => h.BehovId == behovId)
            .OrderByDescending(h => h.Tidspunkt).ThenByDescending(h => h.Id).ToList();

    public IReadOnlyList<Hendelse> HentForRessurs(int ressursId) =>
        _db.Hendelser.AsNoTracking().Where(h => h.RessursId == ressursId)
            .OrderByDescending(h => h.Tidspunkt).ThenByDescending(h => h.Id).ToList();

    // Take(antall) blir LIMIT i SQL-en, så databasen sender bare radene som vises.
    public IReadOnlyList<Hendelse> HentSiste(int antall) =>
        _db.Hendelser.AsNoTracking()
            .OrderByDescending(h => h.Tidspunkt).ThenByDescending(h => h.Id)
            .Take(antall).ToList();
}
