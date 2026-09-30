namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Tallene som vises i "situasjonen akkurat nå"-stripa på forsiden.
/// Controlleren fyller inn tallene, og viewet viser dem.
/// </summary>
public class ForsideViewModel
{
    public int AntallAktiveBehov { get; set; }

    public int AntallAkutteBehov { get; set; }

    public int AntallTilgjengeligeRessurser { get; set; }
}

