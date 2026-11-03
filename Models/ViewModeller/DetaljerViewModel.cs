namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Alt detaljsiden for ett behov trenger (Views/Behov/Detaljer.cshtml):
/// selve behovet og ressursene som er tildelt det.
/// </summary>
public class BehovDetaljerViewModel
{
    public Behov Behov { get; set; } = new();

    // Ressursene som er satt inn på behovet. Tom liste hvis behovet venter.
    public List<Ressurs> TildelteRessurser { get; set; } = new();

    // Hva som har skjedd med behovet, nyeste først.
    public IReadOnlyList<Hendelse> Historikk { get; set; } = new List<Hendelse>();

    // True for den som meldte behovet og for administratorer.
    public bool KanTrekkeTilbake { get; set; }

    // True for administratorer: viser knappene "Tildel ressurs" og "Fjern".
    public bool ErAdmin { get; set; }

    // Forespørsler om ressurser til behovet, nyeste først.
    public List<ForesporselRad> Foresporsler { get; set; } = new();
}

/// <summary>
/// Alt detaljsiden for én ressurs trenger (Views/Ressurs/Detaljer.cshtml).
/// </summary>
public class RessursDetaljerViewModel
{
    public Ressurs Ressurs { get; set; } = new();

    // True hvis ressursen er satt inn på et behov.
    public bool ErTildelt { get; set; }

    // Behovet ressursen er tildelt. Bare fylt ut for offentlige aktører:
    // private aktører får bare vite AT ressursen er tildelt, ikke hvor.
    public Behov? TildeltBehov { get; set; }

    // True for offentlige aktører. Styrer lenker og tekst på siden.
    public bool ErOffentligAktor { get; set; }

    // True for eieren av ressursen og for administratorer. Viser "Trekk tilbake"
    // og eventuell avvisningsgrunn.
    public bool KanTrekkeTilbake { get; set; }

    // Hva som har skjedd med ressursen, nyeste først.
    public IReadOnlyList<Hendelse> Historikk { get; set; } = new List<Hendelse>();

    // True når ressursen er godkjent og ledig, og brukeren er en offentlig aktør.
    // Da vises delen "Forespør til mitt behov" (med en melding hvis brukeren ikke har behov).
    public bool KanForesporres { get; set; }

    // Brukerens egne behov, som ressursen kan forespørres til.
    public List<Behov> MineBehov { get; set; } = new();
}
