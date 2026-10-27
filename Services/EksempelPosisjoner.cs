namespace Beredskapsportal.Services;

/// <summary>
/// Gir eksempeldataene i InMemory-repositoriene en adresse og en plassering på kartet,
/// slik at kartet viser noe helt fra start.
///
/// Alle plasseringene er ekte adresser i Kristiansand, hentet fra Geonorge
/// (samme adresseregister som adressesøket i appen bruker). Da ligger punktene
/// alltid på land, og adressen som vises stemmer med der punktet er på kartet.
///
/// Ekte behov og ressurser får plasseringen sin i registreringsskjemaet, så denne
/// klassen brukes bare til eksempeldata. Kan slettes når vi bytter til MariaDB.
/// </summary>
public static class EksempelPosisjoner
{
    // Åtte spredte adresser i hvert område: (adresse, breddegrad, lengdegrad).
    private static readonly Dictionary<string, (string Adresse, double Breddegrad, double Lengdegrad)[]> Adresser = new()
    {
        ["Sentrum"] =
        [
            ("Rådhusgata 13C", 58.14667, 7.99591),
            ("Markens gate 51", 58.14739, 7.98922),
            ("Vestre Strandgate 21", 58.14351, 7.99319),
            ("Holbergs gate 64", 58.15010, 7.99491),
            ("Dronningens gate 83", 58.14773, 8.00182),
            ("Østre Strandgate 28", 58.14403, 7.99914),
            ("Vestre Strandgate 26", 58.14528, 7.99058),
            ("Kronprinsens gate 37", 58.14948, 7.99894),
        ],
        ["Lund"] =
        [
            ("Johan Øydegards vei 49", 58.15002, 8.01974),
            ("Kommandørveien 9", 58.15090, 8.02890),
            ("Snorres gate 7B", 58.14561, 8.02349),
            ("Johan Øydegards vei 48", 58.15471, 8.02117),
            ("Steinkleiva 5", 58.15178, 8.01163),
            ("Prestvikveien 30", 58.14641, 8.01435),
            ("Tors gate 11", 58.15272, 8.01703),
            ("Teglverksveien 36", 58.14763, 8.02756),
        ],
        ["Vågsbygd"] =
        [
            ("Nordtjønnveien 21B", 58.12539, 7.93995),
            ("Bjørkeliveien 18A", 58.11971, 7.94265),
            ("Hyllebakken 29", 58.12188, 7.93150),
            ("Kronmarka 3", 58.12386, 7.95003),
            ("Nordtjønnåsen 51", 58.12820, 7.93158),
            ("Karussveien 7", 58.12928, 7.94635),
            ("Tverrveien 17", 58.12874, 7.93993),
            ("Grågåsveien 47A", 58.12175, 7.93775),
        ],
        ["Grim"] =
        [
            ("Artillerivollen 55", 58.15510, 7.97630),
            ("Lømslands vei 19", 58.15212, 7.99320),
            ("Egsveien 98", 58.16414, 7.98125),
            ("Jørgen Moes gate 8", 58.14705, 7.97955),
            ("Paradisveien 26", 58.15324, 7.96179),
            ("Andreas Kjærs vei 64", 58.15895, 7.98978),
            ("Møllevannsveien 57", 58.14944, 7.96949),
            ("Banehaven 4B", 58.14862, 7.98734),
        ],
        ["Eg"] =
        [
            ("Kokleheia 25", 58.16499, 7.99303),
            ("Sødal terrasse 24", 58.17234, 7.98527),
            ("Dovreveien 3", 58.15662, 7.99165),
            ("Universitetsveien 3B", 58.16222, 8.00775),
            ("Egsveien 102", 58.16314, 7.97999),
            ("Arenfeldts vei 4", 58.15803, 8.00131),
            ("Kvernveien 28", 58.16992, 7.99328),
            ("Andreas Kjærs vei 90", 58.16027, 7.98655),
        ],
        ["Randesund"] =
        [
            ("Morildveien 39", 58.13192, 8.08432),
            ("Bordalssvingen 35", 58.12987, 8.09848),
            ("Odderheitunet 67", 58.13939, 8.08442),
            ("Kystveien 130A", 58.12459, 8.08639),
            ("Dvergsneskollen 85", 58.13016, 8.07134),
            ("Dvergsnesveien 150", 58.13608, 8.07389),
            ("Indre Fidjekilen 31", 58.13656, 8.09393),
            ("Dvergsnesveien 289", 58.12684, 8.07844),
        ],
        ["Songdalen"] =
        [
            ("Stemtjønnvegen 76", 58.18058, 7.83795),
            ("Stemtjønnvegen 188", 58.18671, 7.84307),
            ("Stemtjønnvegen 27", 58.17792, 7.83244),
            ("Stemtjønnvegen 90", 58.18132, 7.84058),
            ("Stemtjønnvegen 9", 58.17874, 7.83168),
            ("Stemtjønnvegen 75", 58.18037, 7.83763),
        ],
        ["Søm"] =
        [
            ("Korsvikkilen 55", 58.14711, 8.07015),
            ("Fuglevikkleiva 56", 58.14282, 8.05885),
            ("Dvergsnesveien 130", 58.14049, 8.07596),
            ("Bliksheia 9", 58.15042, 8.05795),
            ("Liane ringvei 72", 58.15354, 8.07583),
            ("Snehvitbakken 43", 58.15366, 8.06646),
            ("Sømsveien 204", 58.14261, 8.06789),
            ("Dvergsnesveien 61", 58.14901, 8.07771),
        ],
    };

    // Hvor mange adresser som er brukt i hvert område så langt.
    // Hvert nytt punkt får neste adresse, så to punkter får aldri samme plassering.
    private static readonly Dictionary<string, int> BruktIOmrade = new();

    // Brukes for å hindre at to tråder deler ut samme adresse samtidig.
    private static readonly object LasObjekt = new();

    /// <summary>
    /// Henter neste ledige adresse og plassering i det gitte området.
    /// Ukjent område gir en adresse i sentrum. Er alle adressene i et område
    /// brukt, begynner listen forfra.
    /// </summary>
    public static (string Adresse, double Breddegrad, double Lengdegrad) For(string omrade)
    {
        lock (LasObjekt)
        {
            if (!Adresser.ContainsKey(omrade))
            {
                omrade = "Sentrum";
            }

            var liste = Adresser[omrade];
            var brukt = BruktIOmrade.GetValueOrDefault(omrade, 0);
            BruktIOmrade[omrade] = brukt + 1;

            // % (rest ved deling) gjør at vi starter på nytt hvis listen er brukt opp.
            return liste[brukt % liste.Length];
        }
    }
}
