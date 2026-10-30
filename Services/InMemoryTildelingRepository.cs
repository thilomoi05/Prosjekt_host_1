using Beredskapsportal.Models;

namespace Beredskapsportal.Services;

/// <summary>
/// Midlertidig, minnebasert lagring av tildelinger, med noen eksempeldata.
/// Registrert som Singleton i Program.cs, som de andre InMemory-repositoriene.
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

    /// <summary>
    /// Eksempeldata. Behovene her har status Tildelt i InMemoryBehovRepository,
    /// og ressursene passer til behovet (f.eks. aggregat til nødstrøm).
    /// </summary>

    internal static List<Tildeling> LagEksempelData()
    {
        return new List<Tildeling>
        {
            // Behov 6: Nødstrøm i Vågsbygd  <- ressurs 10: aggregat i Vågsbygd
            new() { BehovId = 6, RessursId = 10, Tidspunkt = new DateTime(2026, 9, 1, 9, 30, 0), TildeltAv = "test" },

            // Behov 3: Kommunikasjonsutstyr på Grim  <- ressurs 19: kommunikasjonsutstyr på Grim
            new() { BehovId = 3, RessursId = 19, Tidspunkt = new DateTime(2026, 9, 1, 11, 15, 0), TildeltAv = "test" },

            // Behov 11: Nødstrøm på Eg  <- ressurs 20: aggregat på Eg
            new() { BehovId = 11, RessursId = 20, Tidspunkt = new DateTime(2026, 8, 30, 14, 0, 0), TildeltAv = "test" },

            // Behov 8: Diesel i Sentrum  <- ressurs 16 (drivstoff) og 8 (transport). Viser at ett behov kan få flere ressurser.
            new() { BehovId = 8, RessursId = 16, Tidspunkt = new DateTime(2026, 9, 1, 8, 0, 0), TildeltAv = "test" },
            new() { BehovId = 8, RessursId = 8, Tidspunkt = new DateTime(2026, 9, 1, 8, 5, 0), TildeltAv = "test", Kommentar = "Transporterer dieselen fra lageret." },
        };
    }
}
