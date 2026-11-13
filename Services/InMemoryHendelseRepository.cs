using Beredskapsportal.Models;

namespace Beredskapsportal.Services;

/// <summary>
/// Minnebasert lagring av historikken. Brukes av enhetstestene.
/// </summary>
public class InMemoryHendelseRepository : IHendelseRepository
{
    private readonly List<Hendelse> _hendelser = new();
    private readonly object _lasObjekt = new();
    private int _nesteId = 1;

    public void LeggTil(Hendelse hendelse)
    {
        lock (_lasObjekt)
        {
            hendelse.Id = _nesteId++;
            _hendelser.Add(hendelse);
        }
    }

    public IReadOnlyList<Hendelse> HentForBehov(int behovId)
    {
        lock (_lasObjekt)
        {
            return _hendelser.Where(h => h.BehovId == behovId)
                .OrderByDescending(h => h.Tidspunkt).ThenByDescending(h => h.Id).ToList();
        }
    }

    public IReadOnlyList<Hendelse> HentForRessurs(int ressursId)
    {
        lock (_lasObjekt)
        {
            return _hendelser.Where(h => h.RessursId == ressursId)
                .OrderByDescending(h => h.Tidspunkt).ThenByDescending(h => h.Id).ToList();
        }
    }

    public IReadOnlyList<Hendelse> HentSiste(int antall)
    {
        lock (_lasObjekt)
        {
            return _hendelser
                .OrderByDescending(h => h.Tidspunkt).ThenByDescending(h => h.Id)
                .Take(antall).ToList();
        }
    }
}
