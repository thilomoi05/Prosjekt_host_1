namespace Beredskapsportal.Models;

/// <summary>
/// Hvor alvorlig/tidskritisk et behov er.
/// Brukes til å sortere og fargekode behov i oversikten.
/// </summary>
public enum Prioritet
{
    Planlagt,
    Akutt
}

/// <summary>
/// Livssyklusen til et registrert behov, fra det meldes inn til det er løst.
/// </summary>
public enum BehovStatus
{
    Ny,
    Venter,
    Tildelt,
    Fullfort
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
