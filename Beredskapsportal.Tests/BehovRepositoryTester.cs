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
}