using Beredskapsportal.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Beredskapsportal.Data;

/// <summary>
/// Koblingen mellom C#-modellene og MariaDB-databasen (Entity Framework Core).
/// Hver DbSet blir en tabell: Behov, Ressurser, Tildelinger, Foresporsler og Hendelser (historikken).
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
    public DbSet<Foresporsel> Foresporsler => Set<Foresporsel>();
    public DbSet<Hendelse> Hendelser => Set<Hendelse>();

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
            behov.Property(b => b.RegistrertAv).HasMaxLength(100);
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
            // Lagres som tekst ("Venter", "Godkjent", "Avvist"), så det er lett å lese i databasen.
            ressurs.Property(r => r.Status).HasConversion<string>().HasMaxLength(40);
            ressurs.Property(r => r.Avvisningsgrunn).HasMaxLength(500);
            ressurs.Ignore(r => r.VisningsNavn);   // regnes ut fra Type, lagres ikke

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

            // En ressurs kan bare være tildelt ett behov om gangen. Den unike indeksen
            // gjør at databasen selv nekter en ny tildeling av samme ressurs.
            tildeling.HasIndex(t => t.RessursId).IsUnique();

            // Fremmednøkler: en tildeling må peke på et behov og en ressurs som finnes.
            tildeling.HasOne<Behov>().WithMany().HasForeignKey(t => t.BehovId);
            tildeling.HasOne<Ressurs>().WithMany().HasForeignKey(t => t.RessursId);

            tildeling.Property(t => t.TildeltAv).HasMaxLength(100);
            tildeling.Property(t => t.Kommentar).HasMaxLength(500);
        });

        modelBuilder.Entity<Foresporsel>(foresporsel =>
        {
            foresporsel.ToTable("Foresporsler");
            foresporsel.Property(f => f.ForespurtAv).HasMaxLength(100);
            foresporsel.Property(f => f.Kommentar).HasMaxLength(500);
            foresporsel.Property(f => f.Status).HasConversion<string>().HasMaxLength(40);
            foresporsel.Property(f => f.Begrunnelse).HasMaxLength(500);
            foresporsel.Property(f => f.BehandletAv).HasMaxLength(100);

            // Fremmednøkler: forespørselen må gjelde et behov og en ressurs som finnes.
            // Trekkes behovet eller ressursen tilbake (slettes), slettes forespørslene også.
            foresporsel.HasOne<Behov>().WithMany().HasForeignKey(f => f.BehovId);
            foresporsel.HasOne<Ressurs>().WithMany().HasForeignKey(f => f.RessursId);
        });

        modelBuilder.Entity<Hendelse>(hendelse =>
        {
            hendelse.ToTable("Hendelser");
            hendelse.Property(h => h.UtfortAv).HasMaxLength(100);
            hendelse.Property(h => h.Beskrivelse).HasMaxLength(500);
            // Gjør det raskt å hente historikken til ett behov eller én ressurs.
            hendelse.HasIndex(h => h.BehovId);
            hendelse.HasIndex(h => h.RessursId);
        });
    }
}