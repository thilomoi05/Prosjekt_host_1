using Beredskapsportal.Models;

namespace Beredskapsportal.Services;

/// <summary>
/// Lagring av forespørsler om ressurser (Foresporsel).
/// </summary>
public interface IForesporselRepository
{
    // Alle forespørsler, nyeste først.
    IReadOnlyList<Foresporsel> HentAlle();

    Foresporsel? HentMedId(int id);

    Foresporsel LeggTil(Foresporsel foresporsel);

    // Godkjenner eller avslår. Begrunnelse er bare satt ved avslag.
    void SettStatus(int id, ForesporselStatus status, string? begrunnelse, string behandletAv);
}
