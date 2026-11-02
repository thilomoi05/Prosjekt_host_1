using Beredskapsportal.Models;

namespace Beredskapsportal.Services;

/// <summary>
/// Lagring av historikken (Hendelse): hva som har skjedd med et behov eller en ressurs, og av hvem.
/// </summary>
public interface IHendelseRepository
{
    void LeggTil(Hendelse hendelse);

    // Historikken til ett behov, nyeste først.
    IReadOnlyList<Hendelse> HentForBehov(int behovId);

    // Historikken til én ressurs, nyeste først.
    IReadOnlyList<Hendelse> HentForRessurs(int ressursId);
}
