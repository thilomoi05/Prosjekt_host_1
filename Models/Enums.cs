namespace Beredskapsportal.Models;

/// <summary>
/// Hvor mye et behov haster:
///  - Hoy:     trengs nå. Liv, helse eller kritiske funksjoner står på spill.
///  - Moderat: trengs snart, i løpet av et døgn.
///  - Lav:     trengs, men kan vente og planlegges.
/// Skrives "Hoy" uten ø i koden. Skjermen viser "Høy" (se PrioritetVisning).
/// </summary>
public enum Prioritet
{
    Lav,
    Moderat,
    Hoy
}

/// <summary>
/// Status for et behov. Bare to steg:
///  - Venter:  behovet er meldt inn, men har ikke fått noen ressurs ennå.
///  - Tildelt: minst én ressurs er tildelt behovet (se Tildeling). Da regnes behovet som løst.
/// </summary>
public enum BehovStatus
{
    Venter,
    Tildelt
}

/// <summary>
/// Hvilken type behov en aktør melder inn. Listen kan utvides etter behov.
/// </summary>
public enum BehovType
{
    Nodstrom,
    Drivstofftransport,
    Kommunikasjonsutstyr,
    Nodbelysning,
    Oppvarming,
    Annet
}

/// <summary>
/// Hvilken type ressurs en tilbyder registrerer.
/// </summary>
public enum RessursType
{
    Aggregat,
    Drivstoff,
    Ups,
    Transport,
    Kommunikasjonsutstyr,
    Annet
}

/// <summary>
/// Rollen en bruker registrerer seg med. Styrer hva slags aktør de representerer
/// (det offentlige som melder behov, eller private/bedrifter som tilbyr ressurser).
/// </summary>
public enum BrukerRolle
{
    OffentligAktor,
    PrivatAktor
}
