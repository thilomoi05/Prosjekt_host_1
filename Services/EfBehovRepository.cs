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

    // Nyeste først, som i den minnebaserte versjonen.
    private IQueryable<Behov> NyesteForst() =>
        _db.Behov.AsNoTracking().OrderByDescending(b => b.Dato).ThenByDescending(b => b.Id);

    public IReadOnlyList<Behov> HentAlle() => NyesteForst().ToList();

    public IReadOnlyList<Behov> HentSiste(int antall) => NyesteForst().Take(antall).ToList();

    public Behov LeggTil(Behov behov)
    {
        _db.Behov.Add(behov);
        _db.SaveChanges();
        return behov;
    }

    public int TellAktive() => _db.Behov.Count(b => b.Status != BehovStatus.Fullfort);

    public int TellAkutte() =>
        _db.Behov.Count(b => b.Prioritet == Prioritet.Akutt && b.Status != BehovStatus.Fullfort);

    public int TellUnderBehandling() =>
        _db.Behov.Count(b => b.Status == BehovStatus.Venter || b.Status == BehovStatus.Tildelt);
}