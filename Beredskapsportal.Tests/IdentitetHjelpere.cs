using Beredskapsportal.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Beredskapsportal.Tests;

/// <summary>
/// Lager "falske" utgaver av Identity sine klasser (UserManager, SignInManager, RoleManager)
/// med NSubstitute, samme måte som i foreleserens eksempel (AccountControllerTests).
/// En falsk klasse gjør ingenting av seg selv: testen bestemmer hva metodene svarer,
/// f.eks. "FindByNameAsync("test") gir denne brukeren". Da trengs ingen database.
/// </summary>
public static class IdentitetHjelpere
{
    public static UserManager<Bruker> LagBrukerManager() =>
        Substitute.For<UserManager<Bruker>>(
            Substitute.For<IUserStore<Bruker>>(),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<Bruker>(),
            Array.Empty<IUserValidator<Bruker>>(),
            Array.Empty<IPasswordValidator<Bruker>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Substitute.For<IServiceProvider>(),
            NullLogger<UserManager<Bruker>>.Instance);

    public static SignInManager<Bruker> LagInnloggingManager(UserManager<Bruker> brukerManager) =>
        Substitute.For<SignInManager<Bruker>>(
            brukerManager,
            Substitute.For<IHttpContextAccessor>(),
            Substitute.For<IUserClaimsPrincipalFactory<Bruker>>(),
            Options.Create(new IdentityOptions()),
            NullLogger<SignInManager<Bruker>>.Instance,
            Substitute.For<IAuthenticationSchemeProvider>(),
            Substitute.For<IUserConfirmation<Bruker>>());

    public static RoleManager<IdentityRole> LagRolleManager() =>
        Substitute.For<RoleManager<IdentityRole>>(
            Substitute.For<IRoleStore<IdentityRole>>(),
            Array.Empty<IRoleValidator<IdentityRole>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            NullLogger<RoleManager<IdentityRole>>.Instance);
}
