using Beredskapsportal.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Beredskapsportal.Data;

/// <summary>
/// Koblingen mellom C#-modellene og MariaDB-databasen (Entity Framework Core).
/// Hver DbSet blir en tabell: Behov, Ressurser og Tildelinger.
///
/// Arver fra IdentityDbContext (ASP.NET Core Identity), som legger til tabellene for
/// brukere og roller: AspNetUsers (våre Bruker-objekter), AspNetRoles, AspNetUserRoles
/// og noen til som Identity bruker internt.
/// </summary>
public class BeredskapsDbContext : IdentityDbContext<Bruker>
{
    public BeredskapsDbContext(DbContextOptions<BeredskapsDbContext> options) : base(options)
    {
    }

    public DbSet<Behov> Behov => Set<Behov>();
    public DbSet<Ressurs> Ressurser => Set<Ressurs>();
    public DbSet<Tildeling> Tildelinger => Set<Tildeling>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Må kalles først: setter opp alle Identity-tabellene (brukere, roller osv.).
        base.OnModelCreating(modelBuilder);

        // Vårt eget felt på brukeren. Resten (brukernavn, e-post, passord-hash) kommer fra Identity.
        modelBuilder.Entity<Bruker>(bruker =>
        {
            bruker.Property(b => b.FulltNavn).HasMaxLength(200);
        });

        modelBuilder.Entity<Behov>(behov =>
        {
            behov.ToTable("Behov");
            behov.Property(b => b.Type).HasConversion<string>().HasMaxLength(40);
            behov.Property(b => b.Prioritet).HasConversion<string>().HasMaxLength(40);
            behov.Property(b => b.Status).HasConversion<string>().HasMaxLength(40);
            behov.Property(b => b.GeografiskOmrade).HasMaxLength(200);
            behov.Property(b => b.Kontaktperson).HasMaxLength(200);
            behov.Property(b => b.Telefon).HasMaxLength(30);
            behov.Ignore(b => b.VisningsNavn);
        });

        modelBuilder.Entity<Ressurs>(ressurs =>
        {
            ressurs.ToTable("Ressurser");
            ressurs.Property(r => r.Type).HasConversion<string>().HasMaxLength(40);
            ressurs.Property(r => r.Adresse).HasMaxLength(300);
            ressurs.Property(r => r.Kontaktperson).HasMaxLength(200);
            ressurs.Property(r => r.Telefon).HasMaxLength(30);
            ressurs.Property(r => r.RegistrertAv).HasMaxLength(100);

            ressurs.Property(r => r.Bilder)
                .HasConversion(
                    liste => string.Join('|', liste),
                    tekst => tekst.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    new ValueComparer<List<string>>(
                        (a, b) => a!.SequenceEqual(b!),
                        liste => liste.Aggregate(0, (hash, fil) => HashCode.Combine(hash, fil.GetHashCode())),
                        liste => liste.ToList()))
                .HasMaxLength(2000);
        });

        modelBuilder.Entity<Tildeling>(tildeling =>
        {
            tildeling.ToTable("Tildelinger");

            // Sammensatt primærnøkkel: samme ressurs kan bare tildeles samme behov én gang.
            tildeling.HasKey(t => new { t.BehovId, t.RessursId });

            // Fremmednøkler: en tildeling må peke på et behov og en ressurs som finnes.
            tildeling.HasOne<Behov>().WithMany().HasForeignKey(t => t.BehovId);
            tildeling.HasOne<Ressurs>().WithMany().HasForeignKey(t => t.RessursId);

            tildeling.Property(t => t.TildeltAv).HasMaxLength(100);
            tildeling.Property(t => t.Kommentar).HasMaxLength(500);
        });
    }
}