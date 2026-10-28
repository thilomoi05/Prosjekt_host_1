using Beredskapsportal.Data;
using Beredskapsportal.Models;
using Microsoft.EntityFrameworkCore;

namespace Beredskapsportal.Services;

/// <summary>
/// Lagrer brukere i MariaDB. Databasen skiller ikke mellom store og små
/// bokstaver (utf8mb4_unicode_ci), så "Test" og "test" er samme brukernavn.
/// </summary>
public class EfBrukerRepository : IBrukerRepository
{
    private readonly BeredskapsDbContext _db;

    public EfBrukerRepository(BeredskapsDbContext db)
    {
        _db = db;
    }

    public Bruker? FinnVedBrukernavn(string brukernavn) =>
        _db.Brukere.AsNoTracking().FirstOrDefault(b => b.Brukernavn == brukernavn);

    public bool BrukernavnErOpptatt(string brukernavn) =>
        _db.Brukere.Any(b => b.Brukernavn == brukernavn);

    public Bruker LeggTil(Bruker bruker)
    {
        _db.Brukere.Add(bruker);
        _db.SaveChanges();
        return bruker;
    }
}