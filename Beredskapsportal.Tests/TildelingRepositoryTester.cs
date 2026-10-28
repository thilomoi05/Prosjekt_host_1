using Beredskapsportal.Models;
using Beredskapsportal.Services;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester for InMemoryTildelingRepository og at eksempeldataene henger sammen med behovene.
/// </summary>
public class TildelingRepositoryTester
{
    [Fact]
    public void LeggTil_NyTildelingKommerMedIHentAlle()
    {
        // Arrange
        var repository = new InMemoryTildelingRepository();
        var antallFor = repository.HentAlle().Count;

        // Act
        repository.LeggTil(new Tildeling { BehovId = 1, RessursId = 5, TildeltAv = "test" });

        // Assert: det er én tildeling mer enn før
        Assert.Equal(antallFor + 1, repository.HentAlle().Count);
    }

    [Fact]
    public void Eksempeldata_AlleTildelteBehovHarStatusTildelt()
    {
        // Arrange
        var behovRepository = new InMemoryBehovRepository();
        var tildelingRepository = new InMemoryTildelingRepository();

        // Act: finn behovene som har en tildeling i eksempeldataene
        var tildelteBehovIder = tildelingRepository.HentAlle().Select(t => t.BehovId).Distinct();

        // Assert: alle disse behovene skal ha status Tildelt
        foreach (var id in tildelteBehovIder)
        {
            var behov = behovRepository.HentAlle().Single(b => b.Id == id);
            Assert.Equal(BehovStatus.Tildelt, behov.Status);
        }
    }
}
