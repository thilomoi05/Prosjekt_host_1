using System.Reflection;
using Beredskapsportal.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace Beredskapsportal.Tests;

/// <summary>
/// Sjekker at sidene med behov, kartet og tildelingene bare er åpne for offentlige aktører,
/// og at adminsiden bare er åpen for Admin.
/// Testen leser [Authorize(Roles = ...)] på controller-klassene, så den feiler
/// hvis noen fjerner eller endrer rollekravet ved et uhell.
/// </summary>
public class TilgangTester
{
    [Theory]
    [InlineData(typeof(BehovController))]
    [InlineData(typeof(KartController))]
    [InlineData(typeof(TildelingController))]
    [InlineData(typeof(ForesporselController))]
    public void Controller_KreverRollenOffentligAktor(Type controllerType)
    {
        // Act: hent [Authorize]-attributtet som står på klassen
        var attributt = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        // Assert: attributtet finnes, og bare OffentligAktor har tilgang
        Assert.NotNull(attributt);
        Assert.Equal("OffentligAktor", attributt!.Roles);
    }

    [Fact]
    public void AdminController_KreverRollenAdmin()
    {
        var attributt = typeof(AdminController).GetCustomAttribute<AuthorizeAttribute>();

        // Bare Admin, ikke offentlige aktører, kan godkjenne og tildele.
        Assert.NotNull(attributt);
        Assert.Equal("Admin", attributt!.Roles);
    }
}
