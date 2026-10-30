using Beredskapsportal.Models;
using Beredskapsportal.Services;
using Microsoft.EntityFrameworkCore;

namespace Beredskapsportal.Data;

/// <summary>
/// Kjører migrations ved oppstart og fyller databasen med eksempeldata
/// første gang, slik at appen viser det samme som den minnebaserte versjonen.
/// </summary>
public static class DatabaseOppsett
{
    public static void Initialiser(IServiceProvider tjenester)
    {
        using var scope = tjenester.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BeredskapsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BeredskapsDbContext>>();

        // Databasen bruker noen sekunder på å starte. Prøver på nytt i ca. 30 sekunder.
        for (var forsok = 1; ; forsok++)
        {
            try
            {
                // Lager eller oppdaterer tabellene ut fra filene i Migrations-mappa.
                db.Database.Migrate();
                break;
            }
            catch (Exception feil) when (forsok < 10)
            {
                logger.LogWarning("Får ikke kontakt med databasen (forsøk {Forsok}/10): {Melding}", forsok, feil.Message);
                Thread.Sleep(TimeSpan.FromSeconds(3));
            }
        }

        if (!db.Brukere.Any())
        {
            db.Brukere.Add(new Bruker
            {
                FulltNavn = "Test Testesen",
                Epost = "test@example.com",
                Brukernavn = "test",
                Passord = "test",
                Rolle = BrukerRolle.OffentligAktor
            });
        }

        if (!db.Behov.Any())
        {
            // Eksempeldataene er sortert nyeste først. Vi legger inn eldste først,
            // slik at høyere Id betyr nyere behov.
            var behov = InMemoryBehovRepository.LagEksempelData();
            behov.Reverse();
            behov.ForEach(b => b.Id = 0);
            db.Behov.AddRange(behov);
        }

        if (!db.Ressurser.Any())
        {
            var ressurser = InMemoryRessursRepository.LagEksempelData();
            ressurser.ForEach(r => r.Id = 0);
            db.Ressurser.AddRange(ressurser);
        }

        db.SaveChanges();
    }
}