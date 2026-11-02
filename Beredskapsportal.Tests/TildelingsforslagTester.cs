using Beredskapsportal.Models;
using Beredskapsportal.Services;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester forslagene til tildeling: riktig type, nærmeste først, og avstandsformelen.
/// </summary>
public class TildelingsforslagTester
{
    // Et nødstrømsbehov i Kristiansand sentrum.
    private static readonly Behov Nodstrom = new() { Type = BehovType.Nodstrom, Breddegrad = 58.146, Lengdegrad = 7.995 };

    private static Ressurs Ressurs(int id, RessursType type, double breddegrad, double lengdegrad) =>
        new() { Id = id, Type = type, Breddegrad = breddegrad, Lengdegrad = lengdegrad, Status = RessursStatus.Godkjent };

    [Fact]
    public void AvstandKm_KristiansandTilOsloErRundt250Km()
    {
        // Kristiansand (58.146, 7.995) til Oslo (59.911, 10.757): ca. 250 km i luftlinje.
        var avstand = Tildelingsforslag.AvstandKm(58.146, 7.995, 59.911, 10.757);

        Assert.InRange(avstand, 240, 260);
    }

    [Fact]
    public void AvstandKm_SammePunktGirNull()
    {
        Assert.Equal(0, Tildelingsforslag.AvstandKm(58.146, 7.995, 58.146, 7.995), precision: 6);
    }

    [Fact]
    public void Lag_TarBareMedPassendeTyper()
    {
        var ressurser = new[]
        {
            Ressurs(1, RessursType.Aggregat, 58.15, 8.0),
            Ressurs(2, RessursType.Transport, 58.15, 8.0),   // passer ikke til nødstrøm
            Ressurs(3, RessursType.Ups, 58.15, 8.0)
        };

        var forslag = Tildelingsforslag.Lag(Nodstrom, ressurser);

        Assert.Equal(new[] { 1, 3 }, forslag.Select(f => f.Ressurs.Id).OrderBy(id => id));
    }

    [Fact]
    public void Lag_NaermesteKommerForst()
    {
        var ressurser = new[]
        {
            Ressurs(1, RessursType.Aggregat, 58.30, 8.0),    // langt unna
            Ressurs(2, RessursType.Aggregat, 58.147, 7.996)  // nesten samme sted
        };

        var forslag = Tildelingsforslag.Lag(Nodstrom, ressurser);

        Assert.Equal(2, forslag[0].Ressurs.Id);
    }

    [Fact]
    public void Lag_VisAlleTyperTarMedAlt()
    {
        var ressurser = new[] { Ressurs(1, RessursType.Transport, 58.15, 8.0) };

        var forslag = Tildelingsforslag.Lag(Nodstrom, ressurser, visAlleTyper: true);

        Assert.Single(forslag);
    }
}
