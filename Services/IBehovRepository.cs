using Beredskapsportal.Models;

namespace Beredskapsportal.Services;

/// <summary>
/// Abstraksjon over lagring av behov, se IBrukerRepository for begrunnelsen
/// bak å bruke et interface her.
/// </summary>
public interface IBehovRepository
{
    IReadOnlyList<Behov> HentAlle();
    IReadOnlyList<Behov> HentSiste(int antall);
    Behov LeggTil(Behov behov);
    int TellAktive();
    int TellUnderBehandling();
}
