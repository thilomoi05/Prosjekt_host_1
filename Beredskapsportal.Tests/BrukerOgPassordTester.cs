using Beredskapsportal.Services;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester passordhashingen og brukeroppslaget som innloggingen bygger på.
/// </summary>
public class BrukerOgPassordTester
{
    [Fact]
    public void VerifiserPassord_GodtarRiktigPassord()
    {
        // Arrange
        var (hash, salt) = PassordHasher.HashPassord("hemmelig123");

        // Act + Assert
        Assert.True(PassordHasher.VerifiserPassord("hemmelig123", hash, salt));
    }

    [Fact]
    public void VerifiserPassord_AvviserFeilPassord()
    {
        var (hash, salt) = PassordHasher.HashPassord("hemmelig123");

        // Stor forbokstav er et annet passord
        Assert.False(PassordHasher.VerifiserPassord("Hemmelig123", hash, salt));
    }

    [Fact]
    public void HashPassord_SammePassordGirUlikHashPaGrunnAvSalt()
    {
        // To brukere med samme passord skal ikke få lik hash
        var (hash1, _) = PassordHasher.HashPassord("passord");
        var (hash2, _) = PassordHasher.HashPassord("passord");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void FinnVedBrukernavn_BryrSegIkkeOmStoreOgSmaBokstaver()
    {
        // Arrange: repositoryet har testbrukeren "test" fra start
        var repository = new InMemoryBrukerRepository();

        // Act
        var bruker = repository.FinnVedBrukernavn("TEST");

        // Assert
        Assert.NotNull(bruker);
        Assert.Equal("test", bruker.Brukernavn);
    }
}
