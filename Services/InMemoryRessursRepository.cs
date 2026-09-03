using Beredskapsportal.Models;

namespace Beredskapsportal.Services;

/// <summary>
/// Midlertidig, minnebasert lagring av ressurser tilbudt av private aktører/bedrifter.
/// Se InMemoryBrukerRepository for begrunnelsen bak den enkle minnebaserte løsningen.
/// </summary>
public class InMemoryRessursRepository : IRessursRepository
{
    private readonly List<Ressurs> _ressurser;
    private readonly object _lasObjekt = new();
    private int _nesteId;

    public InMemoryRessursRepository()
    {
        _ressurser = LagEksempelData();
        _nesteId = _ressurser.Count + 1;
    }

    public IReadOnlyList<Ressurs> HentAlle()
    {
        lock (_lasObjekt)
        {
            return _ressurser.ToList();
        }
    }

    public Ressurs LeggTil(Ressurs ressurs)
    {
        lock (_lasObjekt)
        {
            ressurs.Id = _nesteId++;
            _ressurser.Insert(0, ressurs);
            return ressurs;
        }
    }

    public int TellTilgjengelige()
    {
        lock (_lasObjekt)
        {
            return _ressurser.Count;
        }
    }

    /// <summary>
    /// Genererer 28 eksempelressurser (matcher tallet fra Figma-skissen) fordelt
    /// jevnt på type og område, slik at "Tilgjengelige ressurser" viser noe
    /// realistisk helt fra start.
    /// </summary>
    private static List<Ressurs> LagEksempelData()
    {
        var typer = new[] { RessursType.Aggregat, RessursType.Drivstoff, RessursType.Ups, RessursType.Transport, RessursType.Kommunikasjonsutstyr };
        var omrader = new[] { "Sentrum", "Lund", "Vågsbygd", "Grim", "Eg", "Randesund", "Songdalen", "Søm" };

        var data = new List<Ressurs>();
        for (var i = 1; i <= 28; i++)
        {
            var type = typer[i % typer.Length];
            var omrade = omrader[i % omrader.Length];

            data.Add(new Ressurs
            {
                Id = i,
                Type = type,
                BeskrivelseAvKapasitet = $"Tilbudt {type.ToString().ToLowerInvariant()} fra lokal aktør.",
                GeografiskBase = omrade,
                TilgjengeligFra = new DateOnly(2026, 9, 1),
                TilgjengeligTil = new DateOnly(2026, 9, 30),
                Kontaktperson = "Registrert tilbyder",
                Telefon = "40000000"
            });
        }

        return data;
    }
}
