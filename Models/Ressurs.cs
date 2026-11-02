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

    // Nye ressurser venter på godkjenning fra en administrator (se RessursStatus).
    public RessursStatus Status { get; set; } = RessursStatus.Venter;

    // Hvorfor administratoren avviste ressursen. Vises for eieren. Tom hvis den ikke er avvist.
    public string? Avvisningsgrunn { get; set; }

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

    // Brukernavnet til den som registrerte ressursen. Brukes til å vise private
    // aktører hvilke av deres egne ressurser som er tildelt et behov.
    public string RegistrertAv { get; set; } = string.Empty;

    // Plasseringen på kartet, i vanlige GPS-koordinater (grader).
    // Breddegrad = nord/sør (ca. 58 i Kristiansand), lengdegrad = øst/vest (ca. 8).
    // Settes når brukeren klikker i kartet i registreringsskjemaet.
    public double Breddegrad { get; set; }

    public double Lengdegrad { get; set; }

    /// <summary>
    /// Teksten som vises for ressurstypen, f.eks. "UPS (batteri)" for Ups.
    /// Samlet ett sted, som VisningsNavn på Behov. Lagres ikke i databasen.
    /// </summary>
    public string VisningsNavn => Type == RessursType.Ups ? "UPS (batteri)" : Type.ToString();
}
