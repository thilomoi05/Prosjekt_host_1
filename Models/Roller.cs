namespace Beredskapsportal.Models;

/// <summary>
/// Navnene på rollene i portalen, samlet ett sted (som RoleNames i foreleserens eksempel).
/// Brukes i [Authorize(Roles = ...)] og User.IsInRole(...), så vi slipper tekst spredt rundt i koden.
///
///  - OffentligAktor: melder behov, ser kart, alle ressurser og tildelinger.
///  - PrivatAktor:    tilbyr ressurser og ser bare sine egne.
///  - Admin:          fordeler ressurser til behov og godkjenner offentlige aktører (sidene kommer senere).
///                    En admin har også rollen OffentligAktor, så alle sider for offentlige aktører virker for admin.
/// </summary>
public static class Roller
{
    public const string OffentligAktor = nameof(BrukerRolle.OffentligAktor);
    public const string PrivatAktor = nameof(BrukerRolle.PrivatAktor);
    public const string Admin = "Admin";

    // Alle rollene, brukes når rollene opprettes i databasen ved oppstart (IdentitetOppsett).
    public static readonly string[] Alle = { OffentligAktor, PrivatAktor, Admin };
}
