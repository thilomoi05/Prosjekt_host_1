using Beredskapsportal.Models;

namespace Beredskapsportal.Services;

/// <summary>
/// Én ressurs som foreslås for et behov, med avstanden mellom dem.
/// </summary>
public class RessursForslag
{
    public Ressurs Ressurs { get; set; } = new();

    // Avstand i luftlinje fra behovet til ressursen, i kilometer.
    public double AvstandKm { get; set; }
}

/// <summary>
/// Lager forslag til hvilke ressurser som kan tildeles et behov:
///  1. Ressurser av en type som passer til behovet (se PassendeTyper).
///  2. Sortert med den nærmeste først.
/// Bare ressurser som er godkjent og ikke tildelt, sendes inn hit (se TildelingTjeneste).
/// </summary>
public static class Tildelingsforslag
{
    /// <summary>
    /// Hvilke ressurstyper som passer til hver behovstype. Samlet ett sted, så tabellen er lett
    /// å endre. Behovstypen "Annet" står ikke her: da passer alle typer.
    /// </summary>
    public static readonly Dictionary<BehovType, RessursType[]> PassendeTyper = new()
    {
        [BehovType.Nodstrom] = new[] { RessursType.Aggregat, RessursType.Ups },
        [BehovType.Drivstofftransport] = new[] { RessursType.Drivstoff, RessursType.Transport },
        [BehovType.Kommunikasjonsutstyr] = new[] { RessursType.Kommunikasjonsutstyr },
        [BehovType.Nodbelysning] = new[] { RessursType.Aggregat, RessursType.Ups },
        [BehovType.Oppvarming] = new[] { RessursType.Aggregat }
    };

    /// <summary>True hvis ressurstypen passer til behovstypen.</summary>
    public static bool Passer(BehovType behovType, RessursType ressursType) =>
        !PassendeTyper.TryGetValue(behovType, out var typer) || typer.Contains(ressursType);

    /// <summary>
    /// Avstanden i luftlinje mellom to punkter på jorda, i kilometer (haversine-formelen).
    /// Jorda er rund, så vi kan ikke bare bruke Pytagoras på breddegrad og lengdegrad.
    /// Formelen regner ut vinkelen mellom punktene sett fra jordas sentrum, og ganger med jordradien.
    /// </summary>
    public static double AvstandKm(double breddegrad1, double lengdegrad1, double breddegrad2, double lengdegrad2)
    {
        const double JordradiusKm = 6371;

        // Gradene gjøres om til radianer, som er det Math.Sin og Math.Cos regner med.
        double TilRadianer(double grader) => grader * Math.PI / 180;

        var deltaBredde = TilRadianer(breddegrad2 - breddegrad1);
        var deltaLengde = TilRadianer(lengdegrad2 - lengdegrad1);

        var a = Math.Sin(deltaBredde / 2) * Math.Sin(deltaBredde / 2)
              + Math.Cos(TilRadianer(breddegrad1)) * Math.Cos(TilRadianer(breddegrad2))
              * Math.Sin(deltaLengde / 2) * Math.Sin(deltaLengde / 2);

        return JordradiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    /// <summary>
    /// Forslagene for ett behov: passende ressurser, nærmeste først.
    /// Med visAlleTyper: true kommer alle de ledige ressursene med, uansett type.
    /// </summary>
    public static List<RessursForslag> Lag(Behov behov, IEnumerable<Ressurs> ledigeRessurser, bool visAlleTyper = false)
    {
        return ledigeRessurser
            .Where(ressurs => visAlleTyper || Passer(behov.Type, ressurs.Type))
            .Select(ressurs => new RessursForslag
            {
                Ressurs = ressurs,
                AvstandKm = AvstandKm(behov.Breddegrad, behov.Lengdegrad, ressurs.Breddegrad, ressurs.Lengdegrad)
            })
            .OrderBy(forslag => forslag.AvstandKm)
            .ToList();
    }
}
