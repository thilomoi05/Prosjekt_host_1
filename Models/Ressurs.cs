namespace Beredskapsportal.Models;

/// <summary>
/// Representerer en ressurs (utstyr, transport, kompetanse) som en bedrift,
/// entreprenør eller privatperson tilbyr under en krise.
/// Hvor ressursen er, skal bare vises for Heimevernet og andre autoriserte brukere.
/// </summary>
public class Ressurs
{
    public int Id { get; set; }

    public RessursType Type { get; set; }

    public string BeskrivelseAvKapasitet { get; set; } = string.Empty;

    // Adressen der ressursen står, f.eks. "Kirkegata 5, 4610 Kristiansand".
    public string Adresse { get; set; } = string.Empty;

    // Hvordan man får tak i ressursen, f.eks. "Står i garasjen, kodelås 1234".
    // Kan inneholde sensitiv informasjon, så den vises bare for autoriserte brukere.
    public string Tilgangsbeskrivelse { get; set; } = string.Empty;

    // Filnavnene til bildene som er lastet opp av ressursen.
    // Selve bildene ligger i mappen "Opplastinger/bilder" (se RessursController).
    public List<string> Bilder { get; set; } = new();

    public DateOnly? TilgjengeligFra { get; set; }

    public DateOnly? TilgjengeligTil { get; set; }

    public string Kontaktperson { get; set; } = string.Empty;

    public string Telefon { get; set; } = string.Empty;

    // Plasseringen på kartet, i vanlige GPS-koordinater (grader).
    // Breddegrad = nord/sør (ca. 58 i Kristiansand), lengdegrad = øst/vest (ca. 8).
    // Settes når brukeren klikker i kartet i registreringsskjemaet.
    public double Breddegrad { get; set; }

    public double Lengdegrad { get; set; }
}
