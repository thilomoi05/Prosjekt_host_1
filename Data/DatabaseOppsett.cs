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

        // Testbrukerne er de samme som i InMemoryBrukerRepository.
        if (!db.Brukere.Any())
        {
            db.Brukere.AddRange(
                new Bruker
                {
                    FulltNavn = "Test Testesen",
                    Epost = "test@example.com",
                    Brukernavn = "test",
                    Passord = "test",
                    Rolle = BrukerRolle.OffentligAktor
                },
                new Bruker
                {
                    FulltNavn = "Privat Testesen",
                    Epost = "privat@example.com",
                    Brukernavn = "privat",
                    Passord = "privat",
                    Rolle = BrukerRolle.PrivatAktor
                });
            db.SaveChanges();
        }

        // Eksempeldataene beholder sine Id-er (1, 2, 3 ...), fordi tildelingene
        // under peker på behov og ressurser med akkurat de Id-ene.
        if (!db.Behov.Any())
        {
            db.Behov.AddRange(InMemoryBehovRepository.LagEksempelData());
            db.SaveChanges();
        }

        if (!db.Ressurser.Any())
        {
            db.Ressurser.AddRange(InMemoryRessursRepository.LagEksempelData());
            db.SaveChanges();
        }

        // Tildelingene lagres sist, siden de peker på behov og ressurser som må finnes.
        if (!db.Tildelinger.Any())
        {
            db.Tildelinger.AddRange(InMemoryTildelingRepository.LagEksempelData());
            db.SaveChanges();
        }
    }
}