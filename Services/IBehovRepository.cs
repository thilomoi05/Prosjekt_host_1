using Beredskapsportal.Models;

namespace Beredskapsportal.Services;

/// <summary>
/// Abstraksjon over lagring av behov. Controllerne bruker interfacet, så lagringen kan
/// byttes (MariaDB i appen, minnebasert i testene) uten å endre controllerne.
/// </summary>
public interface IBehovRepository
{
    IReadOnlyList<Behov> HentAlle();
    IReadOnlyList<Behov> HentSiste(int antall);
    Behov LeggTil(Behov behov);
    int TellAktive();
    int TellTildelte();
    int TellVentende(Prioritet prioritet);
    
}
