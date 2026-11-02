using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Beredskapsportal.Data;
using Beredskapsportal.Models;
using Beredskapsportal.Services;
using Beredskapsportal.Sikkerhet;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Fjerner headeren "Server: Kestrel" fra svarene, så angripere ikke ser hvilken webserver vi bruker.
builder.WebHost.ConfigureKestrel(valg => valg.AddServerHeader = false);

// MVC. AutoValidateAntiforgeryTokenAttribute sjekker anti-forgery-tokenet på ALLE
// POST-forespørsler i hele appen (beskyttelse mot CSRF), så ingen kan glemme det på en
// ny metode. Skjemaer laget med <form asp-...> får tokenet automatisk.
builder.Services.AddControllersWithViews(valg =>
    valg.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// Uten dette encoder Razor norske bokstaver (æ, ø, å) som HTML-entiteter
// (f.eks. "&#xF8;" for ø) i sidekilden. Det vises riktig i nettleseren uansett,
// men denne innstillingen gjør at kildekoden også blir ren og lesbar UTF-8.
builder.Services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Latin1Supplement));

// Datalagring: MariaDB. Lokalt kommer tilkoblingsstrengen fra User Secrets, i Docker fra docker-compose.yml.
// Uten tilkoblingsstreng stopper appen med en tydelig melding (Identity trenger databasen).
var tilkoblingsstreng = builder.Configuration.GetConnectionString("BeredskapsDb");
if (string.IsNullOrWhiteSpace(tilkoblingsstreng))
{
    throw new InvalidOperationException(
        "Tilkoblingsstrengen 'BeredskapsDb' er ikke satt. Start med 'docker compose up --build', " +
        "eller sett den med User Secrets (se docs/drift.md).");
}

builder.Services.AddDbContext<BeredskapsDbContext>(options =>
    options.UseMySql(tilkoblingsstreng, new MariaDbServerVersion(new Version(11, 4))));

// Scoped: én databasetilkobling per forespørsel.
builder.Services.AddScoped<IBehovRepository, EfBehovRepository>();
builder.Services.AddScoped<IRessursRepository, EfRessursRepository>();
builder.Services.AddScoped<ITildelingRepository, EfTildelingRepository>();

// ASP.NET Core Identity: brukere, passord og roller, lagret i databasen (BeredskapsDbContext).
builder.Services
    .AddIdentity<Bruker, IdentityRole>(valg =>
    {
        // Passordregler
        valg.Password.RequiredLength = 8;
        valg.Password.RequireDigit = true;
        valg.Password.RequireLowercase = true;
        valg.Password.RequireUppercase = true;
        valg.Password.RequireNonAlphanumeric = false;   // spesialtegn er ikke påkrevd

        // Utestenging: etter 5 feil passord på rad låses kontoen i 5 minutter.
        // Stopper programmer som prøver tusenvis av passord.
        valg.Lockout.MaxFailedAccessAttempts = 5;
        valg.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        valg.Lockout.AllowedForNewUsers = true;

        // Hver e-post kan bare brukes av én bruker. Æ, ø og å er lov i brukernavn.
        valg.User.RequireUniqueEmail = true;
        valg.User.AllowedUserNameCharacters += "æøåÆØÅ";
    })
    .AddEntityFrameworkStores<BeredskapsDbContext>()
    .AddErrorDescriber<NorskeIdentityFeilmeldinger>()      // norske feilmeldinger
    .AddClaimsPrincipalFactory<BrukerClaimsFabrikk>();     // fullt navn i innloggingscookien

// Innloggingscookien som Identity lager når noen logger inn.
builder.Services.ConfigureApplicationCookie(valg =>
{
    valg.LoginPath = "/Konto/LoggInn";
    // Prøver en innlogget bruker å åpne en side rollen ikke har tilgang til
    // (f.eks. en privat aktør på /Behov), sendes de til oversikten.
    valg.AccessDeniedPath = "/Oversikt";

    valg.Cookie.Name = "Beredskapsportal.Innlogging";
    valg.Cookie.HttpOnly = true;                                  // JavaScript kan ikke lese cookien (hjelper mot XSS)
    valg.Cookie.SameSite = SameSiteMode.Lax;                      // sendes ikke med skjemaer fra andre nettsteder (hjelper mot CSRF)
    valg.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;  // bare over HTTPS når siden kjører på HTTPS
    valg.ExpireTimeSpan = TimeSpan.FromHours(8);                  // logges ut etter 8 timer uten aktivitet
    valg.SlidingExpiration = true;                                // tiden starter på nytt når brukeren er aktiv
});

var app = builder.Build();

// Lager tabellene, rollene, testbrukerne og eksempeldataene første gang appen kobler til databasen.
await DatabaseOppsett.InitialiserAsync(app.Services);

// Configure the HTTP request pipeline.
// Sikkerhetsheadere først, så alle svar får dem (Content-Security-Policy, X-Frame-Options osv.).
app.UseMiddleware<SikkerhetsheadereMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Bruker punktum som desimaltegn i hver forespørsel (58.14, ikke 58,14).
// Kartet i skjemaene sender koordinater med punktum. Uten dette ville en PC med
// norske språkinnstillinger ikke klart å lese dem, mens Docker-containeren ville
// klart det. Slik oppfører appen seg likt overalt.
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture),
    SupportedCultures = new[] { CultureInfo.InvariantCulture },
    SupportedUICultures = new[] { CultureInfo.InvariantCulture }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
