using Beredskapsportal.Data;
using Beredskapsportal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester at rollene og testbrukerne lages riktig ved oppstart (IdentitetOppsett).
/// RoleManager og UserManager er falske (se IdentitetHjelpere.cs), så ingen database trengs.
/// </summary>
public class IdentitetOppsettTester
{
    private readonly RoleManager<IdentityRole> _rolleManager = IdentitetHjelpere.LagRolleManager();
    private readonly UserManager<Bruker> _brukerManager = IdentitetHjelpere.LagBrukerManager();

    public IdentitetOppsettTester()
    {
        // Ingen roller eller brukere finnes fra før, og alt som lages, går bra.
        _rolleManager.RoleExistsAsync(Arg.Any<string>()).Returns(false);
        _rolleManager.CreateAsync(Arg.Any<IdentityRole>()).Returns(IdentityResult.Success);
        _brukerManager.FindByNameAsync(Arg.Any<string>()).Returns((Bruker?)null);
        _brukerManager.CreateAsync(Arg.Any<Bruker>(), Arg.Any<string>()).Returns(IdentityResult.Success);
        _brukerManager.AddToRolesAsync(Arg.Any<Bruker>(), Arg.Any<IEnumerable<string>>()).Returns(IdentityResult.Success);
    }

    /// <summary>Kjører oppsettet med (eller uten) passord for testbrukerne.</summary>
    private Task KjorOppsett(string? passord)
    {
        var tjenester = new ServiceCollection()
            .AddSingleton(_rolleManager)
            .AddSingleton(_brukerManager)
            .BuildServiceProvider();

        var innstillinger = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Testbrukere:Passord"] = passord })
            .Build();

        return IdentitetOppsett.OpprettRollerOgTestbrukereAsync(tjenester, innstillinger, NullLogger.Instance);
    }

    [Fact]
    public async Task LagerAlleTreRollene()
    {
        await KjorOppsett(passord: null);

        foreach (var rolle in Roller.Alle)
        {
            await _rolleManager.Received(1).CreateAsync(Arg.Is<IdentityRole>(r => r.Name == rolle));
        }
    }

    [Fact]
    public async Task UtenPassordLagesIngenTestbrukere()
    {
        await KjorOppsett(passord: null);

        await _brukerManager.DidNotReceive().CreateAsync(Arg.Any<Bruker>(), Arg.Any<string>());
    }

    [Fact]
    public async Task AdminFarRollenAdminOgOffentligAktor()
    {
        await KjorOppsett(passord: "Testpassord123");

        await _brukerManager.Received(1).AddToRolesAsync(
            Arg.Is<Bruker>(b => b.UserName == "admin"),
            Arg.Is<IEnumerable<string>>(roller => roller.Contains(Roller.Admin) && roller.Contains(Roller.OffentligAktor)));
    }
}
