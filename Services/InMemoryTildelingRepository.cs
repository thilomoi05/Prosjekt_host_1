using Beredskapsportal.Models;

namespace Beredskapsportal.Services;

/// <summary>
/// Minnebasert lagring av tildelinger, med eksempeldata.
/// Brukes av enhetstestene, og eksempeldataene legges inn i databasen første gang (DatabaseOppsett).
/// </summary>
public class InMemoryTildelingRepository : ITildelingRepository
{
    private readonly List<Tildeling> _tildelinger;
    private readonly object _lasObjekt = new();

    public InMemoryTildelingRepository()
    {
        _tildelinger = LagEksempelData();
    }

    public IReadOnlyList<Tildeling> HentAlle()
    {
        lock (_lasObjekt)
        {
            return _tildelinger.ToList();
        }
    }

    public Tildeling LeggTil(Tildeling tildeling)
    {
        lock (_lasObjekt)
        {
            _tildelinger.Add(tildeling);
            return tildeling;
        }
    }

    public void Fjern(int behovId, int ressursId)
    {
        lock (_lasObjekt)
        {
            _tildelinger.RemoveAll(t => t.BehovId == behovId && t.RessursId == ressursId);
        }
    }

    /// <summary>
    /// Eksempeldata. Behovene her har status Tildelt i InMemoryBehovRepository,
    /// og ressursene passer til behovet (f.eks. aggregat til nødstrøm).
    /// </summary>

    internal static List<Tildeling> LagEksempelData()
    {
        return new List<Tildeling>
        {
            // Behov 6: Nødstrøm i Vågsbygd  <- ressurs 10: aggregat i Vågsbygd
            new() { BehovId = 6, RessursId = 10, Tidspunkt = Eksempeldato.Tid(-1, 9, 30), TildeltAv = "test" },

            // Behov 3: Kommunikasjonsutstyr på Grim  <- ressurs 19: kommunikasjonsutstyr på Grim
            new() { BehovId = 3, RessursId = 19, Tidspunkt = Eksempeldato.Tid(-1, 11, 15), TildeltAv = "test" },

            // Behov 11: Nødstrøm på Eg  <- ressurs 20: aggregat på Eg
            new() { BehovId = 11, RessursId = 20, Tidspunkt = Eksempeldato.Tid(-3, 14, 0), TildeltAv = "test" },

            // Behov 8: Diesel i Sentrum  <- ressurs 16 (drivstoff) og 8 (transport). Viser at ett behov kan få flere ressurser.
            new() { BehovId = 8, RessursId = 16, Tidspunkt = Eksempeldato.Tid(-1, 8, 0), TildeltAv = "test" },
            new() { BehovId = 8, RessursId = 8, Tidspunkt = Eksempeldato.Tid(-1, 8, 5), TildeltAv = "test", Kommentar = "Transporterer dieselen fra lageret." },
        };
    }
}
