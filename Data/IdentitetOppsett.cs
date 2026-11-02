using Beredskapsportal.Models;
using Microsoft.AspNetCore.Identity;

namespace Beredskapsportal.Data;

/// <summary>
/// Lager rollene og testbrukerne i databasen første gang appen starter
/// (som IdentitySeeder i foreleserens eksempel). Kalles fra DatabaseOppsett.
///
/// Testbrukerne:
///  - test   (offentlig aktør)
///  - privat (privat aktør)
///  - admin  (administrator, har også rollen offentlig aktør)
///
/// Passordet står IKKE i koden. Det hentes fra innstillingen "Testbrukere:Passord":
///  - i Docker fra TESTBRUKER_PASSORD i .env (se docker-compose.yml)
///  - lokalt fra User Secrets (se docs/drift.md)
/// Er passordet ikke satt, lages ingen testbrukere. Da kan portalen kjøres uten dem.
/// </summary>
public static class IdentitetOppsett
{
    public static async Task OpprettRollerOgTestbrukereAsync(IServiceProvider tjenester, IConfiguration innstillinger, ILogger logger)
    {
        // RoleManager og UserManager er Identity sine verktøy for roller og brukere.
        // De hasher passordet og sjekker passordreglene for oss.
        var rolleManager = tjenester.GetRequiredService<RoleManager<IdentityRole>>();
        var brukerManager = tjenester.GetRequiredService<UserManager<Bruker>>();

        // 1. Rollene: OffentligAktor, PrivatAktor og Admin
        foreach (var rolle in Roller.Alle)
        {
            if (!await rolleManager.RoleExistsAsync(rolle))
            {
                SjekkResultat(await rolleManager.CreateAsync(new IdentityRole(rolle)), $"Klarte ikke å lage rollen {rolle}.");
            }
        }

        // 2. Testbrukerne, bare hvis passordet er satt
        var passord = innstillinger["Testbrukere:Passord"];
        if (string.IsNullOrWhiteSpace(passord))
        {
            logger.LogWarning("Testbrukere:Passord er ikke satt, så testbrukerne lages ikke. Se docs/drift.md.");
            return;
        }

        await LagTestbrukerAsync(brukerManager, "test", "Test Testesen", "test@example.com", passord, Roller.OffentligAktor);
        await LagTestbrukerAsync(brukerManager, "privat", "Privat Testesen", "privat@example.com", passord, Roller.PrivatAktor);
        // Admin får også rollen OffentligAktor, så alle sidene for offentlige aktører virker for admin.
        await LagTestbrukerAsync(brukerManager, "admin", "Admin Testesen", "admin@example.com", passord, Roller.Admin, Roller.OffentligAktor);
    }

    /// <summary>Lager én testbruker med rollene sine, hvis brukeren ikke finnes fra før.</summary>
    private static async Task LagTestbrukerAsync(UserManager<Bruker> brukerManager, string brukernavn, string fulltNavn,
        string epost, string passord, params string[] roller)
    {
        if (await brukerManager.FindByNameAsync(brukernavn) is not null)
        {
            return;   // finnes allerede
        }

        var bruker = new Bruker
        {
            UserName = brukernavn,
            Email = epost,
            FulltNavn = fulltNavn,
            EmailConfirmed = true
        };

        // CreateAsync hasher passordet og lagrer bare hashen.
        SjekkResultat(await brukerManager.CreateAsync(bruker, passord), $"Klarte ikke å lage testbrukeren {brukernavn}.");
        SjekkResultat(await brukerManager.AddToRolesAsync(bruker, roller), $"Klarte ikke å gi {brukernavn} rollene sine.");
    }

    /// <summary>Stopper oppstarten med en tydelig feilmelding hvis Identity sier nei (f.eks. for svakt passord).</summary>
    private static void SjekkResultat(IdentityResult resultat, string melding)
    {
        if (!resultat.Succeeded)
        {
            throw new InvalidOperationException(melding + " " + string.Join(" ", resultat.Errors.Select(feil => feil.Description)));
        }
    }
}
