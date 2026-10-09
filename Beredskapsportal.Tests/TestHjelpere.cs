using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Beredskapsportal.Tests;

/// <summary>
/// Små hjelpeklasser som testene deler. De erstatter deler av ASP.NET Core
/// som ellers bare finnes når appen faktisk kjører (innlogging, mapper på disk, osv.).
/// </summary>
public static class TestHjelpere
{
    /// <summary>
    /// Kjører de samme valideringsreglene ([Required], [MinLength] osv.) som ASP.NET
    /// kjører før en controller får modellen. Returnerer feilmeldingene som ble funnet.
    /// </summary>
    public static List<ValidationResult> Valider(object modell)
    {
        var feil = new List<ValidationResult>();
        Validator.TryValidateObject(modell, new ValidationContext(modell), feil, validateAllProperties: true);
        return feil;
    }

    /// <summary>
    /// Gir controlleren en "falsk" HttpContext med en innloggingstjeneste som
    /// bare husker hvem som ble logget inn, i stedet for å lage en ekte cookie.
    /// </summary>
    public static FalskInnlogging GiControllerInnlogging(Controller controller)
    {
        var innlogging = new FalskInnlogging();

        // Når controlleren har en HttpContext, henter View() og RedirectToAction()
        // noen MVC-tjenester derfra. AddControllersWithViews registrerer dem, slik Program.cs gjør.
        var tjenesteliste = new ServiceCollection();
        tjenesteliste.AddLogging();
        tjenesteliste.AddControllersWithViews();
        tjenesteliste.AddSingleton<IAuthenticationService>(innlogging);
        var tjenester = tjenesteliste.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = tjenester };
        controller.ControllerContext = new ControllerContext(
            new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor()));

        return innlogging;
    }

    /// <summary>
    /// Lager en opplastet fil (IFormFile) i minnet, slik nettleseren ville sendt den.
    /// </summary>
    public static IFormFile LagBilde(string filnavn, long storrelse = 100)
    {
        var innhold = new MemoryStream(new byte[storrelse]);
        return new FormFile(innhold, 0, storrelse, "Bilder", filnavn);
    }
}

/// <summary>
/// Erstatter den ekte innloggingstjenesten i testene. Husker hvem som logget inn.
/// </summary>
public class FalskInnlogging : IAuthenticationService
{
    public ClaimsPrincipal? InnloggetBruker { get; private set; }

    public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
    {
        InnloggetBruker = principal;
        return Task.CompletedTask;
    }

    public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
    {
        InnloggetBruker = null;
        return Task.CompletedTask;
    }

    public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        => Task.FromResult(AuthenticateResult.NoResult());

    public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        => Task.CompletedTask;

    public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        => Task.CompletedTask;
}

/// <summary>
/// Erstatter IWebHostEnvironment, slik at RessursController lagrer bilder i en
/// midlertidig testmappe i stedet for i prosjektmappen.
/// </summary>
public class FalsktMiljo : IWebHostEnvironment
{
    public FalsktMiljo(string rotmappe)
    {
        ContentRootPath = rotmappe;
        WebRootPath = rotmappe;
    }

    public string WebRootPath { get; set; }
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ApplicationName { get; set; } = "Beredskapsportal.Tests";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; }
    public string EnvironmentName { get; set; } = "Testing";
}
