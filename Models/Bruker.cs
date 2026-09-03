namespace Beredskapsportal.Models;

/// <summary>
/// En registrert bruker av BeredskapsPortalen.
/// Passordet lagres aldri i klartekst - kun det PBKDF2-hashede resultatet
/// (se Services/PassordHasher.cs) sammen med saltet som ble brukt.
/// </summary>
public class Bruker
{
    public int Id { get; set; }

    public string FulltNavn { get; set; } = string.Empty;

    public string Epost { get; set; } = string.Empty;

    public string Brukernavn { get; set; } = string.Empty;

    public string PassordHash { get; set; } = string.Empty;

    public string PassordSalt { get; set; } = string.Empty;

    public BrukerRolle Rolle { get; set; }
}
