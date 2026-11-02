namespace Beredskapsportal.Sikkerhet;

/// <summary>
/// Legger sikkerhetsheadere på hvert svar fra serveren. Headerne forteller nettleseren
/// hva den skal tillate, og beskytter mot angrepene fra forelesningen om
/// webapplikasjonssikkerhet (særlig XSS og clickjacking). Bygget på samme måte som
/// ContentSecurityPolicyMiddleware i foreleserens eksempel.
/// Registreres først i Program.cs (app.UseMiddleware), så alle svar får headerne.
///
/// En middleware er et steg som hver forespørsel går gjennom på vei inn til controlleren.
/// Denne gjør ingenting med forespørselen, den legger bare til headere på svaret.
/// </summary>
public class SikkerhetsheadereMiddleware
{
    private readonly RequestDelegate _neste;
    private readonly IWebHostEnvironment _miljo;

    public SikkerhetsheadereMiddleware(RequestDelegate neste, IWebHostEnvironment miljo)
    {
        _neste = neste;
        _miljo = miljo;
    }

    /// <summary>
    /// Lager Content-Security-Policy: hvor nettleseren får hente skript, stiler, bilder osv. fra.
    /// Alt som ikke står her, blokkeres. Har noen klart å lure inn et skript (XSS),
    /// nekter nettleseren å kjøre det, fordi skript bare får komme fra våre egne .js-filer.
    /// </summary>
    public static string LagInnholdsregler(bool erUtvikling)
    {
        // Under utvikling (dotnet watch) kobler nettleseren seg til en lokal adresse for å
        // laste siden på nytt automatisk. Det tillates bare i utvikling.
        var tilkoblinger = erUtvikling
            ? "connect-src 'self' https://ws.geonorge.no http://localhost:* ws://localhost:* wss://localhost:*"
            : "connect-src 'self' https://ws.geonorge.no";

        return string.Join("; ", new[]
        {
            "default-src 'self'",                                     // alt annet: bare fra vår egen side
            "script-src 'self'",                                      // skript: bare våre egne .js-filer, ingen skript i HTML-en
            "style-src 'self' https://fonts.googleapis.com",          // stiler: site.css, Bootstrap, Leaflet og Google Fonts
            "font-src 'self' https://fonts.gstatic.com",              // skriftfilene til Google Fonts (Outfit)
            "img-src 'self' data: blob: https://cache.kartverket.no", // bilder: egne, små ikoner i CSS (data:), forhåndsvisning av opplastede bilder (blob:) og kartbildene fra Kartverket
            tilkoblinger,                                             // fetch(): egne adresser og adressesøket til Geonorge
            "object-src 'none'",                                      // ingen <object> eller <embed> (gamle programtillegg)
            "base-uri 'self'",                                        // ingen kan bytte grunnadressen til siden med <base>
            "form-action 'self'",                                     // skjemaer kan bare sendes til vår egen side
            "frame-ancestors 'none'"                                  // ingen kan legge siden vår i en ramme (clickjacking)
        });
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // OnStarting kjører rett før svaret sendes. Da kommer headerne med også på feilsider,
        // der ASP.NET tømmer headerne som var satt tidligere (som i foreleserens eksempel).
        context.Response.OnStarting(() =>
        {
            LeggTilHeadere(context.Response.Headers);
            return Task.CompletedTask;
        });

        await _neste(context);
    }

    /// <summary>Legger til alle sikkerhetsheaderne. Egen metode, så testene kan bruke den.</summary>
    public void LeggTilHeadere(IHeaderDictionary headere)
    {
        headere["Content-Security-Policy"] = LagInnholdsregler(_miljo.IsDevelopment());

        // Samme som frame-ancestors over, for eldre nettlesere: siden kan ikke legges i en ramme.
        headere["X-Frame-Options"] = "DENY";

        // Nettleseren skal stole på filtypen serveren oppgir. Hindrer at f.eks. et opplastet
        // "bilde" tolkes og kjøres som skript.
        headere["X-Content-Type-Options"] = "nosniff";

        // Når brukeren følger en lenke til et annet nettsted, sendes bare "localhost:8080",
        // ikke hele adressen (som kan inneholde id-er, f.eks. /Ressurs/Detaljer/7).
        headere["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // XSS-filter i eldre nettlesere (som i forelesningen). Nye nettlesere bruker CSP over.
        headere["X-XSS-Protection"] = "1; mode=block";

        // Portalen bruker ikke kamera, mikrofon eller posisjon, så de slås av.
        headere["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    }
}
