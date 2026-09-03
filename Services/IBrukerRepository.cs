using Beredskapsportal.Models;

namespace Beredskapsportal.Services;

/// <summary>
/// Abstraksjon over brukerlagringen. Ved å programmere mot dette interfacet i
/// stedet for en konkret klasse kan vi seinere bytte ut den enkle
/// minnebaserte lagringen (InMemoryBrukerRepository) med en ekte database
/// uten å endre en eneste linje i Controller-ene.
/// </summary>
public interface IBrukerRepository
{
    Bruker? FinnVedBrukernavn(string brukernavn);
    bool BrukernavnErOpptatt(string brukernavn);
    Bruker LeggTil(Bruker bruker);
}
