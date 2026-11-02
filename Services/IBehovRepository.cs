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

    // Ett behov med gitt id, eller null hvis det ikke finnes (brukes av detaljsiden).
    Behov? HentMedId(int id);
    Behov LeggTil(Behov behov);
    int TellAktive();
    int TellTildelte();
    int TellVentende(Prioritet prioritet);
    
}
