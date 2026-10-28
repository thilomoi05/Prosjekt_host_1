using Beredskapsportal.Controllers;
using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester for siden "Hva trengs", som viser utstyr som trengs gruppert på type.
/// </summary>
public class HvaTrengsControllerTester
{
    [Fact]
    public void Index_TellerBareBehovSomVenter()
    {
        // Arrange
        var repository = new InMemoryBehovRepository();
        var controller = new HvaTrengsController(repository);
        var forventet = repository.HentAlle().Count(b => b.Status == BehovStatus.Venter);

        // Act
        var side = Assert.IsType<ViewResult>(controller.Index());
        var utstyr = Assert.IsType<List<UtstyrSomTrengs>>(side.Model);

        // Assert: summen av alle gruppene er lik antall ventende behov (tildelte er ikke med)
        Assert.Equal(forventet, utstyr.Sum(u => u.Antall));
    }

    [Fact]
    public void Index_ViserHoyPrioritetForst()
    {
        // Arrange
        var controller = new HvaTrengsController(new InMemoryBehovRepository());

        // Act
        var side = Assert.IsType<ViewResult>(controller.Index());
        var utstyr = Assert.IsType<List<UtstyrSomTrengs>>(side.Model);

        // Assert: den første typen i listen har høy prioritet
        Assert.Equal(Prioritet.Hoy, utstyr[0].HoyestePrioritet);
    }
}
