// =========================================================================
// kart.js
// Kjører på kartsiden (Views/Kart/Index.cshtml).
//
// Hva filen gjør:
//  1. Lager kartet (med lagKart fra kartverket.js).
//  2. Henter behov fra serveren (/Kart/Behov) og tegner dem som runde prikker.
//  3. Henter ressurser (/Kart/Ressurser) og tegner dem som firkanter,
//     men bare hvis brukeren har lov (serveren sjekker det også).
//  4. Fyller tabellen under kartet med de samme punktene.
// =========================================================================

const kart = lagKart('kart');
const tabell = document.getElementById('kart-liste');

// Leses fra data-kan-se-ressurser="true/false" på kart-elementet i viewet.
const kanSeRessurser = document.getElementById('kart').dataset.kanSeRessurser === 'true';

// Alle punktene som skal i tabellen samles her før tabellen tegnes.
const punkterTilTabell = [];

/**
 * Lager et kartsymbol med HTML og CSS i stedet for Leaflets standardnål.
 * cssKlasse bestemmer utseendet (se "Kart" i wwwroot/css/site.css).
 */
function lagSymbol(cssKlasse) {
    return L.divIcon({
        className: '',                       // fjerner Leaflets egen stil
        html: '<span class="' + cssKlasse + '"></span>',
        iconSize: [18, 18]
    });
}

/**
 * Gjør tekst trygg å sette inn som HTML. Uten dette kunne noen skrevet
 * <script> i en beskrivelse og fått den kjørt i andres nettleser.
 */
function trygg(tekst) {
    const div = document.createElement('div');
    div.textContent = tekst ?? '';
    return div.innerHTML;
}

/** Henter og tegner alle behov. */
async function visBehov() {
    const svar = await fetch('/Kart/Behov');
    const alleBehov = await svar.json();

    for (const behov of alleBehov) {
        // Akutte behov er røde, planlagte er oransje (samme farger som i oversikten).
        const erAkutt = behov.prioritet === 'Akutt';
        const symbol = lagSymbol(erAkutt ? 'bp-kart-prikk akutt' : 'bp-kart-prikk planlagt');

        L.marker([behov.breddegrad, behov.lengdegrad], { icon: symbol })
            .addTo(kart)
            .bindPopup(
                '<strong>' + trygg(behov.type) + '</strong><br>' +
                trygg(behov.beskrivelse) + '<br>' +
                'Område: ' + trygg(behov.omrade) + '<br>' +
                'Prioritet: ' + trygg(behov.prioritet) + ' · Status: ' + trygg(behov.status)
            );

        punkterTilTabell.push({
            hva: erAkutt ? 'Akutt behov' : 'Planlagt behov',
            type: behov.type,
            sted: behov.omrade,
            beskrivelse: behov.beskrivelse
        });
    }
}

/** Henter og tegner alle ressurser (bare for autoriserte brukere). */
async function visRessurser() {
    const svar = await fetch('/Kart/Ressurser');

    // Hvis serveren sier nei, viser vi rett og slett ingen ressurser.
    if (!svar.ok) {
        return;
    }
    const alleRessurser = await svar.json();

    for (const ressurs of alleRessurser) {
        // Små bilder (miniatyrer) av ressursen. Hvert bilde er en lenke til
        // bildet i full størrelse, som åpnes i en ny fane.
        let bilderHtml = '';
        for (const bildeUrl of ressurs.bilder) {
            bilderHtml += '<a href="' + trygg(bildeUrl) + '" target="_blank">' +
                '<img src="' + trygg(bildeUrl) + '" class="bp-kart-miniatyr" alt="Bilde av ressursen"></a>';
        }

        L.marker([ressurs.breddegrad, ressurs.lengdegrad], { icon: lagSymbol('bp-kart-firkant') })
            .addTo(kart)
            .bindPopup(
                '<strong>' + trygg(ressurs.type) + '</strong><br>' +
                trygg(ressurs.beskrivelse) + '<br>' +
                'Adresse: ' + trygg(ressurs.adresse) + '<br>' +
                'Slik får du tak i den: ' + trygg(ressurs.tilgang) + '<br>' +
                'Kontakt: ' + trygg(ressurs.kontaktperson) + ', tlf. ' + trygg(ressurs.telefon) +
                (bilderHtml ? '<div class="mt-2">' + bilderHtml + '</div>' : '')
            );

        punkterTilTabell.push({
            hva: 'Ressurs',
            type: ressurs.type,
            sted: ressurs.adresse,
            beskrivelse: ressurs.beskrivelse
        });
    }
}

/** Skriver alle innsamlede punkter inn i tabellen under kartet. */
function visTabell() {
    if (punkterTilTabell.length === 0) {
        tabell.innerHTML = '<tr><td colspan="4" class="text-muted">Ingen punkter å vise.</td></tr>';
        return;
    }

    let rader = '';
    for (const punkt of punkterTilTabell) {
        rader += '<tr>' +
            '<td>' + trygg(punkt.hva) + '</td>' +
            '<td>' + trygg(punkt.type) + '</td>' +
            '<td>' + trygg(punkt.sted) + '</td>' +
            '<td>' + trygg(punkt.beskrivelse) + '</td>' +
            '</tr>';
    }
    tabell.innerHTML = rader;
}

/** Starter alt sammen. Kjøres én gang når siden lastes. */
async function start() {
    try {
        await visBehov();
        if (kanSeRessurser) {
            await visRessurser();
        }
        visTabell();
    } catch (feil) {
        console.error(feil);
        tabell.innerHTML = '<tr><td colspan="4" class="text-danger">Klarte ikke å hente punktene til kartet.</td></tr>';
    }
}

start();
