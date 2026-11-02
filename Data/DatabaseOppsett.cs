using Beredskapsportal.Services;
using Microsoft.EntityFrameworkCore;

namespace Beredskapsportal.Data;

/// <summary>
/// Kjører migrations ved oppstart, lager roller og testbrukere (IdentitetOppsett)
/// og fyller databasen med eksempeldata første gang.
/// "async Task" fordi Identity sine metoder for roller og brukere er asynkrone.
/// </summary>
public static class DatabaseOppsett
{
    public static async Task InitialiserAsync(IServiceProvider tjenester)
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
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        // Rollene og testbrukerne (ASP.NET Core Identity)
        var innstillinger = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        await IdentitetOppsett.OpprettRollerOgTestbrukereAsync(scope.ServiceProvider, innstillinger, logger);

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