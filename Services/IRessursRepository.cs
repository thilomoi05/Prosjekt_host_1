using Beredskapsportal.Models;

namespace Beredskapsportal.Services;

/// <summary>
/// Abstraksjon over lagring av ressurser tilbudt av private aktører/bedrifter.
/// </summary>
public interface IRessursRepository
{
    IReadOnlyList<Ressurs> HentAlle();
    Ressurs LeggTil(Ressurs ressurs);
    int TellTilgjengelige();
}
