namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Det profilsiden trenger å vise om den innloggede brukeren.
/// Vi sender IKKE Bruker-objektet direkte til viewet, fordi det
/// inneholder passordet. Viewet skal bare få det det faktisk viser.
/// </summary>
public class ProfilViewModel
{
    public string FulltNavn { get; set; } = string.Empty;
    public string Epost { get; set; } = string.Empty;
    public string Brukernavn { get; set; } = string.Empty;
    public string RolleTekst { get; set; } = string.Empty;
}

