using System.Reflection;
using Beredskapsportal.Controllers;
using Beredskapsportal.Models;
using Beredskapsportal.Sikkerhet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester sikkerhetsheaderne (SikkerhetsheadereMiddleware), rollene, de norske feilmeldingene,
/// rate limiting på innlogging, registrering og profilsiden, og kontrollen av opplastede bilder.
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
        Assert.Equal("Nåværende passord er feil.", feilmeldinger.PasswordMismatch().Description);
    }

    // ---------- Rate limiting ----------

    /// <summary>POST-metoden med dette navnet i controlleren (GET-metoden har samme navn).</summary>
    private static MethodInfo PostMetode(Type controller, string navn) =>
        controller.GetMethods().Single(m => m.Name == navn && m.GetCustomAttribute<HttpPostAttribute>() is not null);

    [Theory]
    [InlineData(nameof(KontoController.LoggInn))]
    [InlineData(nameof(KontoController.Registrer))]
    [InlineData(nameof(KontoController.EndreProfil))]
    [InlineData(nameof(KontoController.EndrePassord))]
    public void Fartsgrense_GjelderAlleSkjemaerSomSjekkerPassord(string metodenavn)
    {
        var attributt = PostMetode(typeof(KontoController), metodenavn).GetCustomAttribute<EnableRateLimitingAttribute>();

        Assert.NotNull(attributt);
        Assert.Equal(Fartsgrenser.Innlogging, attributt.PolicyName);
    }

    [Fact]
    public void Fartsgrense_ErTiForsokPerMinutt()
    {
        Assert.Equal(10, Fartsgrenser.MaksForsok);
        Assert.Equal(TimeSpan.FromMinutes(1), Fartsgrenser.Periode);
    }

    // ---------- Opplastede filer ----------

    [Theory]
    [InlineData(nameof(RessursController.Registrer))]
    [InlineData(nameof(RessursController.Rediger))]
    public void Opplasting_HarGrensePaStorrelsen(string metodenavn)
    {
        var grense = PostMetode(typeof(RessursController), metodenavn).GetCustomAttribute<RequestSizeLimitAttribute>();

        Assert.NotNull(grense);
    }

    [Theory]
    [InlineData(".jpg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0 })]
    [InlineData(".jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 0xDB, 0, 0, 0, 0, 0, 0, 0, 0 })]
    [InlineData(".png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 })]
    [InlineData(".webp", new byte[] { 0x52, 0x49, 0x46, 0x46, 1, 2, 3, 4, 0x57, 0x45, 0x42, 0x50 })]
    public void Bildesjekk_GodtarEkteBilder(string filtype, byte[] innhold)
    {
        Assert.True(Bildesjekk.InnholdPasserMedFiltype(new MemoryStream(innhold), filtype));
    }

    [Fact]
    public void Bildesjekk_AvviserProgramSomHeterJpg()
    {
        // "MZ" er starten på et Windows-program (.exe)
        var program = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0, 0, 0, 0, 0, 0, 0, 0 };

        Assert.False(Bildesjekk.InnholdPasserMedFiltype(new MemoryStream(program), ".jpg"));
    }

    [Fact]
    public void Bildesjekk_AvviserPngSomHeterJpg()
    {
        // Innholdet må passe med endelsen, ikke bare være et eller annet bilde.
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };

        Assert.False(Bildesjekk.InnholdPasserMedFiltype(new MemoryStream(png), ".jpg"));
    }

    [Fact]
    public void Bildesjekk_AvviserTomFil()
    {
        Assert.False(Bildesjekk.InnholdPasserMedFiltype(new MemoryStream(), ".png"));
    }

    [Fact]
    public void Bildesjekk_AvviserAndreFiltyper()
    {
        // SVG er ikke tillatt: det er tekst som kan inneholde JavaScript.
        var svg = "<svg><script>alert(1)</script></svg>"u8.ToArray();

        Assert.False(Bildesjekk.InnholdPasserMedFiltype(new MemoryStream(svg), ".svg"));
    }
}
