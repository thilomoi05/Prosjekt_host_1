using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Beredskapsportal.Data;

/// <summary>
/// Brukes bare av verktøyet "dotnet ef" når vi lager nye migrations
/// (f.eks. dotnet ef migrations add LeggTilIdentity). Ikke brukt når appen kjører.
///
/// Uten denne må verktøyet starte hele appen for å finne databasen, og da må en
/// tilkoblingsstreng være satt. Med denne kan migrations lages uten at databasen kjører,
/// fordi MariaDB-versjonen står her i stedet for å hentes fra databasen.
/// </summary>
public class BeredskapsDbContextFabrikk : IDesignTimeDbContextFactory<BeredskapsDbContext>
{
    public BeredskapsDbContext CreateDbContext(string[] args)
    {
        // Ingen ekte tilkobling skjer når en migration lages, så tilkoblingsstrengen trenger ikke passord.
        var valg = new DbContextOptionsBuilder<BeredskapsDbContext>()
            .UseMySql("Server=localhost;Database=beredskapsportal", new MariaDbServerVersion(new Version(11, 4)))
            .Options;

        return new BeredskapsDbContext(valg);
    }
}
