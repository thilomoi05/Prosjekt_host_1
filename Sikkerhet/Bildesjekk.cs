namespace Beredskapsportal.Sikkerhet;

/// <summary>
/// Sjekker at en opplastet fil faktisk ER et bilde, og ikke bare heter noe som slutter på .jpg.
///
/// Filendelsen bestemmer brukeren selv: et program kan gis navnet "bilde.jpg".
/// Derfor leser vi de første bytene i filen ("magiske tall"). Alle JPG-, PNG- og
/// WEBP-filer starter med faste byte-mønstre, og filen må passe med endelsen sin.
///
/// Brukes av RessursController sammen med sjekken av endelse og størrelse.
/// </summary>
public static class Bildesjekk
{
    // JPG starter med FF D8 FF.
    private static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF };

    // PNG starter med 89 50 4E 47 0D 0A 1A 0A ("\x89PNG\r\n\x1A\n").
    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    // WEBP starter med "RIFF", fire byte med filstørrelse, og så "WEBP".
    private static readonly byte[] Riff = "RIFF"u8.ToArray();
    private static readonly byte[] Webp = "WEBP"u8.ToArray();

    /// <summary>
    /// True hvis innholdet i strømmen passer med filtypen (".jpg", ".jpeg", ".png" eller ".webp").
    /// Leser bare de første 12 bytene.
    /// </summary>
    public static bool InnholdPasserMedFiltype(Stream innhold, string filtype)
    {
        var start = new byte[12];
        var lest = 0;
        // Read kan gi færre byte enn vi ber om, så vi leser til vi har 12 eller filen er slutt.
        while (lest < start.Length)
        {
            var n = innhold.Read(start, lest, start.Length - lest);
            if (n == 0)
            {
                break;
            }
            lest += n;
        }

        return filtype.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => StarterMed(start, lest, Jpeg, 0),
            ".png" => StarterMed(start, lest, Png, 0),
            ".webp" => StarterMed(start, lest, Riff, 0) && StarterMed(start, lest, Webp, 8),
            _ => false
        };
    }

    // True hvis "monster" står i "data" fra posisjon "fra", og vi har lest langt nok.
    private static bool StarterMed(byte[] data, int lest, byte[] monster, int fra) =>
        lest >= fra + monster.Length && data.AsSpan(fra, monster.Length).SequenceEqual(monster);
}
