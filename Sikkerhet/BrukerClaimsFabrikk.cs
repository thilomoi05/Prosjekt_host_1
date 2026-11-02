using System.Security.Claims;
using Beredskapsportal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Beredskapsportal.Sikkerhet;

/// <summary>
/// Bestemmer hva som legges i innloggingscookien når en bruker logger inn.
///
/// Identity legger selv inn brukernavn (ClaimTypes.Name) og rollene (ClaimTypes.Role).
/// Vi legger i tillegg inn det fulle navnet som ClaimTypes.GivenName, fordi menyen,
/// forsiden og Tilby ressurs-skjemaet viser navnet ("Velkommen tilbake, Test").
/// Da slipper vi å hente brukeren fra databasen på hver side.
/// </summary>
public class BrukerClaimsFabrikk : UserClaimsPrincipalFactory<Bruker, IdentityRole>
{
    public BrukerClaimsFabrikk(UserManager<Bruker> userManager, RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> valg)
        : base(userManager, roleManager, valg)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Bruker bruker)
    {
        // Først det Identity lager selv (navn, id og roller) ...
        var identitet = await base.GenerateClaimsAsync(bruker);

        // ... så det fulle navnet vårt.
        identitet.AddClaim(new Claim(ClaimTypes.GivenName, bruker.FulltNavn));
        return identitet;
    }
}
