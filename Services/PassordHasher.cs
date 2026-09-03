using System.Security.Cryptography;

namespace Beredskapsportal.Services;

/// <summary>
/// Enkel hjelpeklasse for trygg passordhåndtering.
/// Bruker PBKDF2 (innebygd i .NET, ingen eksterne avhengigheter) med et unikt
/// tilfeldig salt per bruker, slik at passord aldri lagres eller sammenlignes i klartekst.
/// </summary>
public static class PassordHasher
{
    private const int SaltStorrelseBytes = 16;
    private const int HashStorrelseBytes = 32;
    private const int Iterasjoner = 100_000;

    /// <summary>
    /// Genererer et nytt tilfeldig salt og hasher passordet med det.
    /// </summary>
    public static (string Hash, string Salt) HashPassord(string passord)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(SaltStorrelseBytes);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(passord, saltBytes, Iterasjoner, HashAlgorithmName.SHA256, HashStorrelseBytes);

        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    /// <summary>
    /// Sjekker om et innskrevet passord stemmer med den lagrede hashen, gitt det lagrede saltet.
    /// </summary>
    public static bool VerifiserPassord(string passord, string lagretHash, string lagretSalt)
    {
        var saltBytes = Convert.FromBase64String(lagretSalt);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(passord, saltBytes, Iterasjoner, HashAlgorithmName.SHA256, HashStorrelseBytes);

        return CryptographicOperations.FixedTimeEquals(hashBytes, Convert.FromBase64String(lagretHash));
    }
}
