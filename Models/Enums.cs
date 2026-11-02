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
/// Status for en ressurs som er sendt inn. Bare godkjente ressurser vises for
/// offentlige aktører, så falske innsendinger ikke når frem før en administrator har sett dem.
///  - Venter:   sendt inn, men ikke sjekket av en administrator ennå.
///  - Godkjent: sjekket og godkjent. Vises på kartet og kan tildeles behov.
///  - Avvist:   avvist av en administrator, med en begrunnelse eieren kan se.
/// Om ressursen er tildelt et behov, står ikke her. Det ser vi i tabellen Tildelinger.
/// </summary>
public enum RessursStatus
{
    Venter,
    Godkjent,
    Avvist
}

/// <summary>
/// Status for en forespørsel fra en offentlig aktør om å få en ressurs til sitt behov.
///  - Venter:   sendt, men ikke behandlet av en administrator ennå.
///  - Godkjent: administratoren sa ja, og ressursen er tildelt behovet.
///  - Avslatt:  administratoren sa nei (med begrunnelse), eller forespørselen ble lukket
///              fordi ressursen gikk til et annet behov eller ble trukket tilbake.
/// Skrives "Avslatt" uten å i koden. Skjermen viser "Avslått".
/// </summary>
public enum ForesporselStatus
{
    Venter,
    Godkjent,
    Avslatt
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
/// Rollen en bruker kan velge når de registrerer seg: det offentlige som melder behov,
/// eller private/bedrifter som tilbyr ressurser. Admin er bevisst ikke med her, så ingen
/// kan registrere seg selv som administrator. Rollenavnene i databasen står i Roller.cs.
/// </summary>
public enum BrukerRolle
{
    OffentligAktor,
    PrivatAktor
}
