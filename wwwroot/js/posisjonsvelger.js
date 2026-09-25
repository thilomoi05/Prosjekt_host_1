// =========================================================================
// posisjonsvelger.js
// Et lite kart i registreringsskjemaene (behov og ressurs) der brukeren
// markerer hvor behovet eller ressursen er.
//
// Hvordan det virker:
//  - Skjemaet har to skjulte felt: Breddegrad og Lengdegrad.
//  - Brukeren kan klikke i kartet. Da settes en markør der, og koordinatene
//    skrives inn i de skjulte feltene.
//  - I ressursskjemaet kan brukeren også skrive en adresse og trykke
//    "Finn på kartet". Da spør vi Kartverket hvor adressen er, og setter
//    markøren der. Brukeren sjekker så at den står riktig.
//  - Når skjemaet sendes, følger koordinatene med til serveren som vanlige felt.
// =========================================================================

const velgerKart = lagKart('posisjonsvelger');
const breddegradFelt = document.getElementById('Breddegrad');
const lengdegradFelt = document.getElementById('Lengdegrad');

// Disse finnes bare i ressursskjemaet. I behovsskjemaet blir de null,
// og da hopper vi over adressesøket (se "if" lenger ned).
const adresseFelt = document.getElementById('Adresse');
const finnAdresseKnapp = document.getElementById('finn-adresse');
const adresseMelding = document.getElementById('adresse-melding');
const bekreftBoks = document.getElementById('PlasseringBekreftet');

// Markøren som viser valgt plassering. null = ingen plassering valgt ennå.
let markor = null;

/** Setter (eller flytter) markøren og oppdaterer de skjulte feltene. */
function velgPosisjon(breddegrad, lengdegrad) {
    if (markor === null) {
        markor = L.marker([breddegrad, lengdegrad]).addTo(velgerKart);
    } else {
        markor.setLatLng([breddegrad, lengdegrad]);
    }

    // toFixed(6) gir 6 desimaler, som er nøyaktig nok (ca. 10 cm).
    breddegradFelt.value = breddegrad.toFixed(6);
    lengdegradFelt.value = lengdegrad.toFixed(6);

    // Markøren er flyttet, så brukeren må bekrefte den nye plasseringen på nytt.
    if (bekreftBoks) {
        bekreftBoks.checked = false;
    }
}

// Leaflet kaller denne funksjonen hver gang brukeren klikker i kartet.
velgerKart.on('click', function (hendelse) {
    velgPosisjon(hendelse.latlng.lat, hendelse.latlng.lng);
});

/**
 * Spør Kartverkets gratis adresse-API hvor adressen er, og flytter markøren dit.
 * Svaret ser omtrent slik ut:
 *   { "adresser": [ { "adressetekst": "Kirkegata 5", "poststed": "KRISTIANSAND",
 *                     "representasjonspunkt": { "lat": 58.14, "lon": 7.99 } } ] }
 */
async function finnAdresse() {
    const adresse = adresseFelt.value.trim();
    if (adresse === '') {
        adresseMelding.textContent = 'Skriv inn en adresse først.';
        return;
    }

    adresseMelding.textContent = 'Søker ...';

    try {
        // encodeURIComponent gjør mellomrom, æ, ø, å osv. trygge å ha i en nettadresse.
        const url = 'https://ws.geonorge.no/adresser/v1/sok?treffPerSide=1&sok=' + encodeURIComponent(adresse);
        const svar = await fetch(url);
        const data = await svar.json();

        if (data.adresser.length === 0) {
            adresseMelding.textContent = 'Fant ikke adressen. Sjekk skrivemåten, eller klikk i kartet der ressursen er.';
            return;
        }

        // Vi bruker det første (beste) treffet.
        const treff = data.adresser[0];
        const punkt = treff.representasjonspunkt;

        velgPosisjon(punkt.lat, punkt.lon);
        velgerKart.setView([punkt.lat, punkt.lon], 17);   // zoomer inn så brukeren ser gata

        adresseMelding.textContent =
            'Fant: ' + treff.adressetekst + ', ' + treff.postnummer + ' ' + treff.poststed +
            '. Står markøren riktig? Hvis ikke, klikk der ressursen faktisk er. ' +
            'Kryss så av for at plasseringen er riktig.';
    } catch (feil) {
        console.error(feil);
        adresseMelding.textContent = 'Klarte ikke å søke etter adressen akkurat nå. Klikk i kartet der ressursen er.';
    }
}

// Adressesøket kobles bare på hvis skjemaet har adressefelt (ressursskjemaet).
if (finnAdresseKnapp) {
    finnAdresseKnapp.addEventListener('click', finnAdresse);

    // Trykker brukeren Enter i adressefeltet, søker vi i stedet for å sende skjemaet.
    adresseFelt.addEventListener('keydown', function (hendelse) {
        if (hendelse.key === 'Enter') {
            hendelse.preventDefault();
            finnAdresse();
        }
    });
}

// Hvis skjemaet ble sendt med feil (f.eks. manglende telefonnummer),
// har feltene allerede verdier. Da viser vi markøren der igjen.
if (breddegradFelt.value && lengdegradFelt.value) {
    const bredde = parseFloat(breddegradFelt.value);
    const lengde = parseFloat(lengdegradFelt.value);
    const varBekreftet = bekreftBoks ? bekreftBoks.checked : false;

    velgPosisjon(bredde, lengde);
    velgerKart.setView([bredde, lengde], 16);

    // velgPosisjon fjerner avkrysningen, men her er plasseringen ikke endret.
    if (bekreftBoks) {
        bekreftBoks.checked = varBekreftet;
    }
}
