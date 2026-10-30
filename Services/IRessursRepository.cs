using Beredskapsportal.Models;

namespace Beredskapsportal.Services;

/// <summary>
/// Abstraksjon over lagring av ressurser tilbudt av private aktører/bedrifter.
/// </summary>
public interface IRessursRepository
{
    IReadOnlyList<Ressurs> HentAlle();

    // Én ressurs med gitt id, eller null hvis den ikke finnes (brukes av detaljsiden).
    Ressurs? HentMedId(int id);
    Ressurs LeggTil(Ressurs ressurs);
    int TellTilgjengelige();
}
