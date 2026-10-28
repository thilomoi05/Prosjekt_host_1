// =========================================================================
// kart.js
// Kjører på kartsiden (Views/Kart/Index.cshtml).
//
// Hva filen gjør:
//  1. Lager kartet (med lagKart fra kartverket.js).
//  2. Henter behov fra serveren (/Kart/Behov) og tegner dem som kartnåler.
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

// Ikonene (form + symbol for type) lages i kartikoner.js.

/**
 * Gjør tekst trygg å sette inn som HTML. Uten dette kunne noen skrevet
 * <script> i en beskrivelse og fått den kjørt i andres nettleser.
 */
function trygg(tekst) {
    const div = document.createElement('div');
    div.textContent = tekst ?? '';
    return div.innerHTML;
}

/**
 * Lager knappen "Se detaljer". Brukes både i boblen på kartet og i listen.
 * ekstraKlasser er ekstra CSS-klasser, f.eks. 'w-100 mt-2' for full bredde i boblen.
 * Foreløpig gjør knappen ingenting. id-en er lagt på knappen (data-id),
 * slik at vi senere kan sende brukeren til en side med detaljer om punktet.
 */
function seDetaljerKnapp(id, ekstraKlasser = '') {
    return '<button type="button" class="btn btn-sm btn-outline-primary bp-kart-detaljer ' + ekstraKlasser + '" ' +
        'data-id="' + trygg(String(id)) + '" title="Kommer senere">Se detaljer &rarr;</button>';
}

// Innstillinger for boblen som åpnes når man klikker på et punkt.
// className kobler boblen til stilen "Boble på kartet" i site.css.
const BOBLE_VALG = { minWidth: 280, maxWidth: 320, className: 'bp-boble-popup' };

/**
 * Lager innholdet i boblen som åpnes når man klikker på et punkt.
 * Brukes av både behov og ressurser, så boblene ser like ut.
 *
 * punkt er et objekt med:
 *   form, typeKode, fargeKlasse  ikonet øverst (samme som på kartet)
 *   merkelapp                    f.eks. "Høy prioritet" eller "Ressurs"
 *   tittel, undertittel          typen og beskrivelsen
 *   rader                        liste med [etikett, verdi], f.eks. ['Område', 'Lund']
 *   ekstraHtml                   ferdig HTML under radene (bildene), kan være tom
 *   id                           brukes av "Se detaljer"-knappen
 */
function lagBoble(punkt) {
    // Hver rad blir en etikett (grå, til venstre) og en verdi (til høyre).
    let raderHtml = '';
    for (const [etikett, verdi] of punkt.rader) {
        raderHtml += '<dt>' + trygg(etikett) + '</dt><dd>' + trygg(verdi) + '</dd>';
    }

    return '<div class="bp-boble">' +
        // Toppen: ikon, fargelapp, tittel og undertittel.
        // fargeKlasse setter fargen (currentColor) på både ikonet og fargelappen.
        '<div class="bp-boble-topp ' + punkt.fargeKlasse + '">' +
            '<div class="bp-boble-ikon">' + lagIkonSvg(punkt.form, punkt.typeKode, 40) + '</div>' +
            '<div>' +
                '<span class="bp-boble-merkelapp">' + trygg(punkt.merkelapp) + '</span>' +
                '<div class="bp-boble-tittel">' + trygg(punkt.tittel) + '</div>' +
                '<div class="bp-boble-undertittel">' + trygg(punkt.undertittel) + '</div>' +
            '</div>' +
        '</div>' +
        // Radene med opplysninger. <dl> er HTML for "liste med etiketter og verdier".
        '<dl class="bp-boble-rader">' + raderHtml + '</dl>' +
        (punkt.ekstraHtml ?? '') +
        seDetaljerKnapp(punkt.id, 'w-100 mt-3') +
        '</div>';
}

/** Henter og tegner alle behov. */
async function visBehov() {
    const svar = await fetch('/Kart/Behov');
    const alleBehov = await svar.json();

    for (const behov of alleBehov) {
        // Hver prioritet har sin farge (se "Kartikoner" i site.css).
        // behov.prioritet er "hoy", "moderat" eller "lav", behov.prioritetTekst er "Høy", "Moderat" eller "Lav".
        const fargeKlasse = 'bp-ikon-' + behov.prioritet;
        const merkelapp = behov.prioritetTekst + ' prioritet';

        L.marker([behov.breddegrad, behov.lengdegrad], { icon: lagKartikon('behov', behov.typeKode, fargeKlasse) })
            .addTo(kart)
            .bindPopup(lagBoble({
                form: 'behov',
                typeKode: behov.typeKode,
                fargeKlasse: fargeKlasse,
                merkelapp: merkelapp,
                tittel: behov.type,
                undertittel: behov.beskrivelse,
                rader: [
                    ['Område', behov.omrade],
                    ['Prioritet', behov.prioritetTekst],
                    ['Status', behov.status]
                ],
                id: behov.id
            }), BOBLE_VALG);

        punkterTilTabell.push({
            id: behov.id,
            // Samme ikon som på kartet, brukes i listen.
            form: 'behov',
            typeKode: behov.typeKode,
            fargeKlasse: fargeKlasse,
            hva: merkelapp,
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

        L.marker([ressurs.breddegrad, ressurs.lengdegrad], { icon: lagKartikon('ressurs', ressurs.typeKode, 'bp-ikon-ressurs') })
            .addTo(kart)
            .bindPopup(lagBoble({
                form: 'ressurs',
                typeKode: ressurs.typeKode,
                fargeKlasse: 'bp-ikon-ressurs',
                merkelapp: 'Ressurs',
                tittel: ressurs.type,
                undertittel: ressurs.beskrivelse,
                rader: [
                    ['Adresse', ressurs.adresse],
                    ['Slik får du tak i den', ressurs.tilgang],
                    ['Kontakt', ressurs.kontaktperson + ', tlf. ' + ressurs.telefon]
                ],
                // Bildene er allerede gjort trygge over, så de legges inn som HTML.
                ekstraHtml: bilderHtml ? '<div class="mt-2">' + bilderHtml + '</div>' : '',
                id: ressurs.id
            }), BOBLE_VALG);

        punkterTilTabell.push({
            id: ressurs.id,
            // Samme ikon som på kartet, brukes i listen.
            form: 'ressurs',
            typeKode: ressurs.typeKode,
            fargeKlasse: 'bp-ikon-ressurs',
            hva: 'Ressurs',
            type: ressurs.type,
            sted: ressurs.adresse,
            beskrivelse: ressurs.beskrivelse
        });
    }
}

/** Skriver alle innsamlede punkter inn i tabellen under kartet. */
function visTabell() {
    // Viser antall punkter i overskriften på listen.
    document.getElementById('kart-antall').textContent = punkterTilTabell.length;

    if (punkterTilTabell.length === 0) {
        tabell.innerHTML = '<tr><td colspan="5" class="text-muted">Ingen punkter å vise.</td></tr>';
        return;
    }

    let rader = '';
    for (const punkt of punkterTilTabell) {
        rader += '<tr>' +
            // Ikonet foran teksten (litt mindre enn på kartet), så listen og kartet ser like ut.
            '<td><span class="bp-kartikon ' + punkt.fargeKlasse + '">' +
                lagIkonSvg(punkt.form, punkt.typeKode, 24) + '</span> ' + trygg(punkt.hva) + '</td>' +
            '<td>' + trygg(punkt.type) + '</td>' +
            '<td>' + trygg(punkt.sted) + '</td>' +
            '<td>' + trygg(punkt.beskrivelse) + '</td>' +
            '<td class="text-end">' + seDetaljerKnapp(punkt.id, 'text-nowrap') + '</td>' +
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
        tabell.innerHTML = '<tr><td colspan="5" class="text-danger">Klarte ikke å hente punktene til kartet.</td></tr>';
    }
}

start();
