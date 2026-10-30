using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Beredskapsportal.Data;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Uten dette encoder Razor norske bokstaver (æ, ø, å) som HTML-entiteter
// (f.eks. "&#xF8;" for ø) i sidekilden. Det vises riktig i nettleseren uansett,
// men denne innstillingen gjør at kildekoden også blir ren og lesbar UTF-8.
builder.Services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Latin1Supplement));

// Datalagring: MariaDB hvis en tilkoblingsstreng er satt, ellers minnebasert.
// Lokalt kommer tilkoblingsstrengen fra User Secrets, i Docker fra docker-compose.yml.
// Uten tilkoblingsstreng kjører appen med minnebasert lagring som før.
var tilkoblingsstreng = builder.Configuration.GetConnectionString("BeredskapsDb");
var brukerDatabase = !string.IsNullOrWhiteSpace(tilkoblingsstreng);

if (brukerDatabase)
{
    builder.Services.AddDbContext<BeredskapsDbContext>(options =>
        options.UseMySql(tilkoblingsstreng!, new MariaDbServerVersion(new Version(11, 4))));

    // Scoped: én databasetilkobling per forespørsel.
    builder.Services.AddScoped<IBrukerRepository, EfBrukerRepository>();
    builder.Services.AddScoped<IBehovRepository, EfBehovRepository>();
    builder.Services.AddScoped<IRessursRepository, EfRessursRepository>();
}
else
{
    // Singleton: de minnebaserte lagrene må deles av alle forespørsler,
    // ellers forsvinner data mellom sidevisninger.
    builder.Services.AddSingleton<IBrukerRepository, InMemoryBrukerRepository>();
    builder.Services.AddSingleton<IBehovRepository, InMemoryBehovRepository>();
    builder.Services.AddSingleton<IRessursRepository, InMemoryRessursRepository>();
}

// Enkel cookie-basert innlogging: når en bruker logger inn får de en signert
// informasjonskapsel som identifiserer dem på senere forespørsler. Ukjente/
// uinnloggede besøkende som prøver å nå en [Authorize]-beskyttet side blir
// sendt til innloggingssiden.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Konto/LoggInn";
        options.AccessDeniedPath = "/Konto/LoggInn";
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
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
