using Beredskapsportal.Models;
using Beredskapsportal.Services;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester for tellemetodene i InMemoryBehovRepository, som tall-stripa på forsiden bruker.
/// </summary>
public class BehovRepositoryTester
{
    [Fact]
    public void TellAktive_TellerIkkeFullforteBehov()
    {
        // Arrange: lag et repository og noter hvor mange aktive behov det har fra før
        var repository = new InMemoryBehovRepository();
        var antallFor = repository.TellAktive();

        // Act: legg til et behov som allerede er fullført
        repository.LeggTil(new Behov { Status = BehovStatus.Fullfort, Prioritet = Prioritet.Planlagt });

        // Assert: antallet aktive behov skal være uendret
        Assert.Equal(antallFor, repository.TellAktive());
    }

    [Fact]
    public void TellAkutte_OkerNarNyttAkuttBehovLeggesTil()
    {
        // Arrange
        var repository = new InMemoryBehovRepository();
        var antallFor = repository.TellAkutte();

        // Act: legg til et nytt, akutt behov
        repository.LeggTil(new Behov { Status = BehovStatus.Ny, Prioritet = Prioritet.Akutt });

        // Assert: antallet akutte behov skal ha økt med 1
        Assert.Equal(antallFor + 1, repository.TellAkutte());
    }

    [Fact]
    public void HentSiste_ViserNyesteBehovForst()
    {
        // Arrange
        var repository = new InMemoryBehovRepository();

        // Act
        var nytt = repository.LeggTil(new Behov { Beskrivelse = "Helt nytt" });
        var siste = repository.HentSiste(5);

        // Assert: det nye behovet ligger øverst under "Siste meldte behov", og vi får 5
        Assert.Same(nytt, siste[0]);
        Assert.Equal(5, siste.Count);
    }

    [Fact]
    public void TellUnderBehandling_TellerVenterMenIkkeNy()
    {
        // Arrange
        var repository = new InMemoryBehovRepository();
        var antallFor = repository.TellUnderBehandling();

        // Act: ett behov som venter, og ett som er helt nytt
        repository.LeggTil(new Behov { Status = BehovStatus.Venter });
        repository.LeggTil(new Behov { Status = BehovStatus.Ny });

        // Assert: bare det som venter, telles som "under behandling"
        Assert.Equal(antallFor + 1, repository.TellUnderBehandling());
    }
}