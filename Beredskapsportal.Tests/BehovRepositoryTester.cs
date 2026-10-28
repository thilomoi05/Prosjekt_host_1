using Beredskapsportal.Models;
using Beredskapsportal.Services;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester for tellemetodene i InMemoryBehovRepository, som tall-stripa på forsiden bruker.
/// </summary>
public class BehovRepositoryTester
{
    [Fact]
    public void TellAktive_TellerIkkeTildelteBehov()
    {
        // Arrange: lag et repository og noter hvor mange aktive behov det har fra før
        var repository = new InMemoryBehovRepository();
        var antallFor = repository.TellAktive();

        // Act: legg til et behov som allerede har fått en ressurs (tildelt)
        repository.LeggTil(new Behov { Status = BehovStatus.Tildelt, Prioritet = Prioritet.Lav });

        // Assert: antallet aktive (ventende) behov skal være uendret
        Assert.Equal(antallFor, repository.TellAktive());
    }

    [Fact]
    public void TellVentende_OkerNarNyttBehovMedHoyPrioritetLeggesTil()
    {
        // Arrange
        var repository = new InMemoryBehovRepository();
        var antallFor = repository.TellVentende(Prioritet.Hoy);

        // Act: legg til et nytt behov med høy prioritet
        repository.LeggTil(new Behov { Status = BehovStatus.Venter, Prioritet = Prioritet.Hoy });

        // Assert: antallet høyt prioriterte behov skal ha økt med 1
        Assert.Equal(antallFor + 1, repository.TellVentende(Prioritet.Hoy));
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
    public void TellTildelte_TellerBareTildelteBehov()
    {
        // Arrange
        var repository = new InMemoryBehovRepository();
        var antallFor = repository.TellTildelte();

        // Act: ett behov som er tildelt, og ett som venter
        repository.LeggTil(new Behov { Status = BehovStatus.Tildelt });
        repository.LeggTil(new Behov { Status = BehovStatus.Venter });

        // Assert: bare det tildelte behovet telles
        Assert.Equal(antallFor + 1, repository.TellTildelte());
    }
}