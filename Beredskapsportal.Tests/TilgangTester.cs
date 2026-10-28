using System.Reflection;
using Beredskapsportal.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace Beredskapsportal.Tests;

/// <summary>
/// Sjekker at sidene med behov og kartet bare er åpne for offentlige aktører.
/// Testen leser [Authorize(Roles = ...)] på controller-klassene, så den feiler
/// hvis noen fjerner eller endrer rollekravet ved et uhell.
/// </summary>
public class TilgangTester
{
    [Theory]
    [InlineData(typeof(BehovController))]
    [InlineData(typeof(KartController))]
    public void Controller_KreverRollenOffentligAktor(Type controllerType)
    {
        // Act: hent [Authorize]-attributtet som står på klassen
        var attributt = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        // Assert: attributtet finnes, og bare OffentligAktor har tilgang
        Assert.NotNull(attributt);
        Assert.Equal("OffentligAktor", attributt!.Roles);
    }
}
