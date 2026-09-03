namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Samler alt dashbordet ("Oversikt") trenger å vise, slik at Controller-en
/// kan bygge én ferdig modell i stedet for at View-et henter data selv.
/// </summary>
public class OversiktViewModel
{
    public int AntallAktiveBehov { get; set; }

    public int AntallTilgjengeligeRessurser { get; set; }

    public int AntallUnderBehandling { get; set; }

    public List<Behov> SisteBehov { get; set; } = new();
}
