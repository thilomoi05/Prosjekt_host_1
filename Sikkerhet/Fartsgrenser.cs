using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Beredskapsportal.Sikkerhet;

/// <summary>
/// Rate limiting (fartsgrense) for innlogging, registrering og skjemaene på profilsiden.
///
/// Utestengingen i Identity (Program.cs) låser ÉN konto etter 5 feil passord. Den hjelper ikke
/// når noen prøver ett passord mot mange ulike brukernavn, eller lager mange kontoer.
/// Fartsgrensen stopper det: hver IP-adresse får maks 10 forsøk per minutt på
/// POST /Konto/LoggInn, /Konto/Registrer, /Konto/EndreProfil og /Konto/EndrePassord til sammen.
/// Profilsidene er med fordi de sjekker passordet, så de kunne ellers brukes til å gjette det.
/// Forsøk nummer 11 får svaret 429 Too Many Requests, uten at passordet sjekkes.
///
/// Brukes med [EnableRateLimiting(Fartsgrenser.Innlogging)] på metodene i KontoController.
/// </summary>
public static class Fartsgrenser
{
    // Navnet på regelen. Samme tekst i Program.cs og i attributtet på controlleren.
    public const string Innlogging = "innlogging";

    public const int MaksForsok = 10;
    public static readonly TimeSpan Periode = TimeSpan.FromMinutes(1);

    /// <summary>Legger til fartsgrensene. Kalles fra Program.cs.</summary>
    public static IServiceCollection LeggTilFartsgrenser(this IServiceCollection tjenester) =>
        tjenester.AddRateLimiter(valg =>
        {
            valg.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Én teller per IP-adresse. Fast vindu: telleren nullstilles hvert minutt.
            valg.AddPolicy(Innlogging, kontekst => RateLimitPartition.GetFixedWindowLimiter(
                kontekst.Connection.RemoteIpAddress?.ToString() ?? "ukjent",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = MaksForsok,
                    Window = Periode,
                    QueueLimit = 0   // ingen kø: forsøk over grensen avvises med en gang
                }));

            // En kort forklaring på norsk i stedet for en tom side.
            valg.OnRejected = async (kontekst, avbryt) =>
            {
                kontekst.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
                await kontekst.HttpContext.Response.WriteAsync(
                    "For mange forsøk på kort tid. Vent ett minutt og prøv igjen.", avbryt);
            };
        });
}
