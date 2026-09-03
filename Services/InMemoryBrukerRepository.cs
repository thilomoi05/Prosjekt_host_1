using Beredskapsportal.Models;

namespace Beredskapsportal.Services;

/// <summary>
/// Midlertidig, minnebasert lagring av brukere.
/// Registrert som Singleton i Program.cs slik at samme liste deles av alle
/// forespørsler så lenge nettsiden kjører. Dette er et bevisst enkelt
/// førsteutkast - når prosjektet trenger ekte persistens, byttes denne
/// klassen ut med en database-backet implementasjon av IBrukerRepository.
/// </summary>
public class InMemoryBrukerRepository : IBrukerRepository
{
    private readonly List<Bruker> _brukere = new();
    private readonly object _lasObjekt = new();
    private int _nesteId = 1;

    public Bruker? FinnVedBrukernavn(string brukernavn)
    {
        lock (_lasObjekt)
        {
            return _brukere.FirstOrDefault(b =>
                string.Equals(b.Brukernavn, brukernavn, StringComparison.OrdinalIgnoreCase));
        }
    }

    public bool BrukernavnErOpptatt(string brukernavn) => FinnVedBrukernavn(brukernavn) is not null;

    public Bruker LeggTil(Bruker bruker)
    {
        lock (_lasObjekt)
        {
            bruker.Id = _nesteId++;
            _brukere.Add(bruker);
            return bruker;
        }
    }
}
