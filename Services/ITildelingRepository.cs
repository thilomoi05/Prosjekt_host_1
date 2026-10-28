using Beredskapsportal.Models;

namespace Beredskapsportal.Services;

/// <summary>
/// Lagring av tildelinger (hvilke ressurser som er satt inn på hvilke behov).
/// Samme mønster som IBehovRepository og IRessursRepository, så den kan byttes
/// til en database senere uten å endre controllerne.
/// </summary>
public interface ITildelingRepository
{
    IReadOnlyList<Tildeling> HentAlle();
    Tildeling LeggTil(Tildeling tildeling);
}
