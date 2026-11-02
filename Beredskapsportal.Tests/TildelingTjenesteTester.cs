using Beredskapsportal.Models;
using Beredskapsportal.Services;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester reglene i TildelingTjeneste: godkjenning, tildeling, fjerning og tilbaketrekking.
/// Bruker de minnebaserte repositoriene med eksempeldata.
/// </summary>
public class TildelingTjenesteTester
{
    private readonly InMemoryBehovRepository _behov = new();
    private readonly InMemoryRessursRepository _ressurser = new();
    private readonly InMemoryTildelingRepository _tildelinger = new();
    private readonly InMemoryHendelseRepository _hendelser = new();
    private readonly InMemoryForesporselRepository _foresporsler = new();
    private readonly TildelingTjeneste _tjeneste;

    public TildelingTjenesteTester()
    {
        _tjeneste = new TildelingTjeneste(_behov, _ressurser, _tildelinger, _hendelser, _foresporsler);
    }

    // Et behov som venter, og en godkjent ressurs som ikke er tildelt (fra eksempeldataene).
    private Behov VentendeBehov() => _behov.HentAlle().First(b => b.Status == BehovStatus.Venter);
    private Ressurs LedigRessurs() => _tjeneste.LedigeRessurser().First();

    [Fact]
    public void Tildel_BehovetBlirTildeltOgRessursenErIkkeLedigLenger()
    {
        var behov = VentendeBehov();
        var ressurs = LedigRessurs();

        var feil = _tjeneste.Tildel(behov.Id, new[] { ressurs.Id }, "Hentes kl. 14", "admin");

        Assert.Null(feil);
        Assert.Equal(BehovStatus.Tildelt, _behov.HentMedId(behov.Id)!.Status);
        Assert.DoesNotContain(_tjeneste.LedigeRessurser(), r => r.Id == ressurs.Id);
        Assert.NotEmpty(_hendelser.HentForBehov(behov.Id));   // historikken er skrevet
    }

    [Fact]
    public void Tildel_RessursSomAlleredeErTildeltAvvises()
    {
        // Ressurs 10 er tildelt behov 6 i eksempeldataene. En ressurs kan bare ha ett behov.
        var behov = VentendeBehov();

        var feil = _tjeneste.Tildel(behov.Id, new[] { 10 }, null, "admin");

        Assert.NotNull(feil);
        Assert.Equal(BehovStatus.Venter, _behov.HentMedId(behov.Id)!.Status);
    }

    [Fact]
    public void Tildel_RessursSomIkkeErGodkjentAvvises()
    {
        var behov = VentendeBehov();
        var ny = _ressurser.LeggTil(new Ressurs { Type = RessursType.Aggregat, Status = RessursStatus.Venter });

        var feil = _tjeneste.Tildel(behov.Id, new[] { ny.Id }, null, "admin");

        Assert.NotNull(feil);
    }

    [Fact]
    public void FjernTildeling_SisteRessursGjorBehovetVentendeIgjen()
    {
        // Behov 6 har bare ressurs 10 i eksempeldataene.
        _tjeneste.FjernTildeling(6, 10, "admin");

        Assert.Equal(BehovStatus.Venter, _behov.HentMedId(6)!.Status);
        Assert.Contains(_tjeneste.LedigeRessurser(), r => r.Id == 10);
    }

    [Fact]
    public void FjernTildeling_BehovMedFlereRessurserForblirTildelt()
    {
        // Behov 8 har ressurs 16 og 8 i eksempeldataene.
        _tjeneste.FjernTildeling(8, 16, "admin");

        Assert.Equal(BehovStatus.Tildelt, _behov.HentMedId(8)!.Status);
    }

    [Fact]
    public void TrekkTilbakeBehov_SletterBehovetOgGjorRessurseneLedige()
    {
        _tjeneste.TrekkTilbakeBehov(8, "test");

        Assert.Null(_behov.HentMedId(8));
        Assert.Contains(_tjeneste.LedigeRessurser(), r => r.Id == 16);
        Assert.Contains(_tjeneste.LedigeRessurser(), r => r.Id == 8);
    }

    [Fact]
    public void TrekkTilbakeRessurs_SletterRessursenOgBehovetVenterIgjen()
    {
        // Ressurs 10 er den eneste ressursen på behov 6.
        _tjeneste.TrekkTilbakeRessurs(10, "privat");

        Assert.Null(_ressurser.HentMedId(10));
        Assert.Equal(BehovStatus.Venter, _behov.HentMedId(6)!.Status);
    }

    [Fact]
    public void Avvis_LagrerBegrunnelsen()
    {
        var ny = _ressurser.LeggTil(new Ressurs { Type = RessursType.Aggregat, Status = RessursStatus.Venter });

        _tjeneste.Avvis(ny.Id, "Bildet viser ikke et aggregat.", "admin");

        var lagret = _ressurser.HentMedId(ny.Id)!;
        Assert.Equal(RessursStatus.Avvist, lagret.Status);
        Assert.Equal("Bildet viser ikke et aggregat.", lagret.Avvisningsgrunn);
    }

    // ---------------------------------------------------------------------
    // Forespørsler
    // ---------------------------------------------------------------------

    [Fact]
    public void Foresporr_LagrerEnForesporselSomVenter()
    {
        var behov = VentendeBehov();
        var ressurs = LedigRessurs();

        var feil = _tjeneste.Foresporr(behov.Id, ressurs.Id, "Trengs før kl. 18", "test");

        Assert.Null(feil);
        var foresporsel = Assert.Single(_foresporsler.HentAlle());
        Assert.Equal(ForesporselStatus.Venter, foresporsel.Status);
    }

    [Fact]
    public void Foresporr_SammeRessursToGangerAvvises()
    {
        var behov = VentendeBehov();
        var ressurs = LedigRessurs();
        _tjeneste.Foresporr(behov.Id, ressurs.Id, null, "test");

        var feil = _tjeneste.Foresporr(behov.Id, ressurs.Id, null, "test");

        Assert.NotNull(feil);
        Assert.Single(_foresporsler.HentAlle());
    }

    [Fact]
    public void Foresporr_RessursSomIkkeErLedigAvvises()
    {
        // Ressurs 10 er allerede tildelt behov 6.
        var feil = _tjeneste.Foresporr(VentendeBehov().Id, 10, null, "test");

        Assert.NotNull(feil);
    }

    [Fact]
    public void GodkjennForesporsel_TildelerOgLukkerAndreForesporslerOmSammeRessurs()
    {
        // To behov spør om samme ressurs.
        var ventende = _behov.HentAlle().Where(b => b.Status == BehovStatus.Venter).Take(2).ToList();
        var ressurs = LedigRessurs();
        _tjeneste.Foresporr(ventende[0].Id, ressurs.Id, null, "test");
        _tjeneste.Foresporr(ventende[1].Id, ressurs.Id, null, "test");
        var forste = _foresporsler.HentAlle().Single(f => f.BehovId == ventende[0].Id);

        var feil = _tjeneste.GodkjennForesporsel(forste.Id, "admin");

        Assert.Null(feil);
        Assert.Equal(BehovStatus.Tildelt, _behov.HentMedId(ventende[0].Id)!.Status);
        Assert.Equal(ForesporselStatus.Godkjent, _foresporsler.HentMedId(forste.Id)!.Status);
        var andre = _foresporsler.HentAlle().Single(f => f.BehovId == ventende[1].Id);
        Assert.Equal(ForesporselStatus.Avslatt, andre.Status);   // ressursen gikk til et annet behov
    }

    [Fact]
    public void AvslaForesporsel_LagrerBegrunnelsen()
    {
        var behov = VentendeBehov();
        _tjeneste.Foresporr(behov.Id, LedigRessurs().Id, null, "test");
        var foresporsel = _foresporsler.HentAlle().Single();

        _tjeneste.AvslaForesporsel(foresporsel.Id, "Ressursen trengs et annet sted.", "admin");

        var lagret = _foresporsler.HentMedId(foresporsel.Id)!;
        Assert.Equal(ForesporselStatus.Avslatt, lagret.Status);
        Assert.Equal("Ressursen trengs et annet sted.", lagret.Begrunnelse);
        Assert.Equal(BehovStatus.Venter, _behov.HentMedId(behov.Id)!.Status);
    }

    [Fact]
    public void TrekkTilbakeRessurs_LukkerForesporslerOmRessursen()
    {
        var ressurs = LedigRessurs();
        _tjeneste.Foresporr(VentendeBehov().Id, ressurs.Id, null, "test");

        _tjeneste.TrekkTilbakeRessurs(ressurs.Id, "privat");

        Assert.Equal(ForesporselStatus.Avslatt, _foresporsler.HentAlle().Single().Status);
    }
}
