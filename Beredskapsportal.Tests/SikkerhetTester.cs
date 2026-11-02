using Beredskapsportal.Models;
using Beredskapsportal.Sikkerhet;
using Microsoft.AspNetCore.Http;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester sikkerhetsheaderne (SikkerhetsheadereMiddleware), rollene og de norske feilmeldingene.
/// </summary>
public class SikkerhetTester
{
    /// <summary>Kjører middlewaren sin header-metode og returnerer headerne den satte.</summary>
    private static IHeaderDictionary HeadereFor(string miljo)
    {
        var middleware = new SikkerhetsheadereMiddleware(_ => Task.CompletedTask,
            new FalsktMiljo(Path.GetTempPath()) { EnvironmentName = miljo });
        var context = new DefaultHttpContext();

        middleware.LeggTilHeadere(context.Response.Headers);
        return context.Response.Headers;
    }

    [Theory]
    [InlineData("X-Frame-Options", "DENY")]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("Referrer-Policy", "strict-origin-when-cross-origin")]
    [InlineData("X-XSS-Protection", "1; mode=block")]
    public void Middleware_SetterSikkerhetsheadere(string header, string forventet)
    {
        var headere = HeadereFor("Production");

        Assert.Equal(forventet, headere[header].ToString());
    }

    [Fact]
    public void Innholdsregler_TillaterBareEgneSkript()
    {
        var regler = SikkerhetsheadereMiddleware.LagInnholdsregler(erUtvikling: false);

        // Ingen 'unsafe-inline': skript skrevet rett inn i HTML-en (f.eks. fra XSS) kjøres ikke.
        Assert.Contains("script-src 'self'", regler);
        Assert.DoesNotContain("unsafe-inline", regler);
        Assert.Contains("frame-ancestors 'none'", regler);
        Assert.Contains("form-action 'self'", regler);
    }

    [Fact]
    public void Innholdsregler_LokaleTilkoblingerBareIUtvikling()
    {
        Assert.Contains("ws://localhost:*", SikkerhetsheadereMiddleware.LagInnholdsregler(erUtvikling: true));
        Assert.DoesNotContain("localhost", SikkerhetsheadereMiddleware.LagInnholdsregler(erUtvikling: false));
    }

    [Fact]
    public void Roller_AdminKanIkkeVelgesVedRegistrering()
    {
        // Registreringsskjemaet bruker BrukerRolle. Admin skal ikke finnes der.
        Assert.DoesNotContain(Roller.Admin, Enum.GetNames<BrukerRolle>());
        Assert.Contains(Roller.Admin, Roller.Alle);
    }

    [Fact]
    public void Feilmeldinger_ErPaNorsk()
    {
        var feilmeldinger = new NorskeIdentityFeilmeldinger();

        Assert.Equal("Passordet må være på minst 8 tegn.", feilmeldinger.PasswordTooShort(8).Description);
        Assert.Equal("Brukernavnet «test» er allerede i bruk.", feilmeldinger.DuplicateUserName("test").Description);
    }
}
