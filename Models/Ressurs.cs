namespace Beredskapsportal.Models;

/// <summary>
/// Representerer en ressurs (utstyr, transport, kompetanse) som en privat aktør
/// eller bedrift tilbyr for å avhjelpe strømbruddet.
/// </summary>
public class Ressurs
{
    public int Id { get; set; }

    public RessursType Type { get; set; }

    public string BeskrivelseAvKapasitet { get; set; } = string.Empty;

    public string GeografiskBase { get; set; } = string.Empty;

    public DateOnly? TilgjengeligFra { get; set; }

    public DateOnly? TilgjengeligTil { get; set; }

    public string Kontaktperson { get; set; } = string.Empty;

    public string Telefon { get; set; } = string.Empty;
}
