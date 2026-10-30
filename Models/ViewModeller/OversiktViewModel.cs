namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Samler alt dashbordet ("Oversikt") trenger å vise, slik at Controller-en
/// kan bygge én ferdig modell i stedet for at View-et henter data selv.
/// </summary>
public class OversiktViewModel
{
    // Behov som venter på en ressurs.
    public int AntallAktiveBehov { get; set; }

    // Fordelingen av de ventende behovene på prioritet.
    public int AntallHoy { get; set; }
    public int AntallModerat { get; set; }
    public int AntallLav { get; set; }

    // Ressurser som ikke er tildelt noe behov ennå.
    public int AntallTilgjengeligeRessurser { get; set; }

    // Behov som har fått minst én ressurs.
    public int AntallTildelteBehov { get; set; }

    // Behov med høy prioritet som venter på en ressurs.
    public List<Behov> KreverOppmerksomhet { get; set; } = new();

    // De nyeste behovene som venter på en ressurs.
    public List<Behov> SisteBehov { get; set; } = new();

    // For offentlige aktører: de fem behovene som sist fikk ressurser, med ressursene som er satt inn på dem.
    public List<TildeltBehov> TildelteBehov { get; set; } = new();

    // For private aktører: alle ressursene brukeren har registrert, og om de er tildelt.
    public List<MinRessurs> MineRessurser { get; set; } = new();
}

/// <summary>Ett behov og ressursene som er tildelt det.</summary>
public class TildeltBehov
{
    public Behov Behov { get; set; } = new();
    public List<Ressurs> Ressurser { get; set; } = new();
}

/// <summary>
/// En av brukerens egne ressurser. Private aktører får bare vite om ressursen er
/// tildelt et behov, ikke hvilket behov det er.
/// </summary>
public class MinRessurs
{
    public Ressurs Ressurs { get; set; } = new();
    public bool ErTildelt { get; set; }
}
