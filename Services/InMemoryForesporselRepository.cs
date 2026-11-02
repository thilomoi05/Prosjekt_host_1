using Beredskapsportal.Models;

namespace Beredskapsportal.Services;

/// <summary>
/// Minnebasert lagring av forespørsler. Brukes av enhetstestene.
/// </summary>
public class InMemoryForesporselRepository : IForesporselRepository
{
    private readonly List<Foresporsel> _foresporsler = new();
    private readonly object _lasObjekt = new();
    private int _nesteId = 1;

    public IReadOnlyList<Foresporsel> HentAlle()
    {
        lock (_lasObjekt)
        {
            return _foresporsler.OrderByDescending(f => f.Tidspunkt).ThenByDescending(f => f.Id).ToList();
        }
    }

    public Foresporsel? HentMedId(int id)
    {
        lock (_lasObjekt)
        {
            return _foresporsler.FirstOrDefault(f => f.Id == id);
        }
    }

    public Foresporsel LeggTil(Foresporsel foresporsel)
    {
        lock (_lasObjekt)
        {
            foresporsel.Id = _nesteId++;
            _foresporsler.Add(foresporsel);
            return foresporsel;
        }
    }

    public void SettStatus(int id, ForesporselStatus status, string? begrunnelse, string behandletAv)
    {
        lock (_lasObjekt)
        {
            var foresporsel = _foresporsler.FirstOrDefault(f => f.Id == id);
            if (foresporsel is not null)
            {
                foresporsel.Status = status;
                foresporsel.Begrunnelse = begrunnelse;
                foresporsel.BehandletAv = behandletAv;
            }
        }
    }
}
