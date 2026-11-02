using Beredskapsportal.Models;

namespace Beredskapsportal.Services;

/// <summary>
/// Lagring av tildelinger (hvilke ressurser som er satt inn på hvilke behov).
/// Samme mønster som IBehovRepository og IRessursRepository.
/// </summary>
public interface ITildelingRepository
{
    IReadOnlyList<Tildeling> HentAlle();
    Tildeling LeggTil(Tildeling tildeling);

    // Fjerner koblingen mellom ett behov og én ressurs. Ressursen blir ledig igjen.
    void Fjern(int behovId, int ressursId);
}
