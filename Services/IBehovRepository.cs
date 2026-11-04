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

    // Ett behov med gitt id, eller null hvis det ikke finnes (brukes av detaljsiden).
    Behov? HentMedId(int id);
    Behov LeggTil(Behov behov);

    // Endrer status (Venter eller Tildelt). Brukes når ressurser tildeles eller fjernes.
    void SettStatus(int id, BehovStatus status);

    // Lagrer endringer fra skjemaet "Endre behov". Id-en i behovet sier hvilket behov som endres.
    // Status, dato og hvem som meldte det endres ikke. Gjør ingenting hvis behovet ikke finnes.
    void Oppdater(Behov behov);

    // Sletter behovet. Brukes når den som meldte det, trekker det tilbake.
    void Slett(int id);
    int TellAktive();
    int TellTildelte();
    int TellVentende(Prioritet prioritet);
    
}
