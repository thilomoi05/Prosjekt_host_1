using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Beredskapsportal.Models.ViewModeller;

/// <summary>
/// Data i skjemaet "Endre ressurs" (RessursController.Rediger).
/// Arver alle feltene og valideringsreglene fra RessursViewModel, så reglene for
/// å registrere og å endre en ressurs alltid er de samme.
///
/// Forskjellen er bildene: ved endring er det frivillig å laste opp nye.
/// Laster brukeren opp nye bilder, erstatter de de gamle. Ellers beholdes de gamle.
/// </summary>
public class RedigerRessursViewModel : RessursViewModel
{
    // Hvilken ressurs som endres. Kommer fra adressen (/Ressurs/Rediger/7).
    public int Id { get; set; }

    // Filnavnene til bildene ressursen har nå. Bare for visning i skjemaet.
    // Fylles alltid inn på nytt fra databasen av controlleren, så brukeren kan
    // ikke lure til seg andres bilder ved å endre dette feltet.
    [BindNever]
    public List<string> EksisterendeBilder { get; set; } = new();

    // Status før endringen, så skjemaet kan si om ressursen må godkjennes på nytt.
    // [BindNever]: verdien tas aldri fra skjemaet, bare fra databasen.
    [BindNever]
    public RessursStatus Status { get; set; }
}
