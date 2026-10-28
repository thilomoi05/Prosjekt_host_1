// =========================================================================
// kart.js
// Kjører på kartsiden (Views/Kart/Index.cshtml).
//
// Hva filen gjør:
//  1. Lager kartet (med lagKart fra kartverket.js).
//  2. Henter behov fra serveren (/Kart/Behov) og tegner dem som kartnåler.
//  3. Henter ressurser (/Kart/Ressurser) og tegner dem som firkanter,
//     men bare hvis brukeren har lov (serveren sjekker det også).
//  4. Lager filterpanelet til venstre (typer, prioritet, skjul tildelte).
//  5. Viser bare punktene som passer filteret, både på kartet og i listen.
// =========================================================================

const kart = lagKart('kart');

// Zoomknappene flyttes til høyre, så de ikke havner bak filterpanelet til venstre.
kart.zoomControl.setPosition('topright');
const tabell = document.getElementById('kart-liste');

// Leses fra data-kan-se-ressurser="true/false" på kart-elementet i viewet.
const kanSeRessurser = document.getElementById('kart').dataset.kanSeRessurser === 'true';

// Alle punktene som skal i tabellen samles her før tabellen tegnes.
const punkterTilTabell = [];

// Leses fra adressen, f.eks. /Kart?liste=ressurser.
//  - "ressurser": listen åpnes med en gang og viser bare tilgjengelige ressurser
//    (brukes av kortet "Tilgjengelige ressurser" på oversikten).
//  - "alle": listen åpnes med en gang og viser alle punktene.
//  - ingenting: kartet vises som vanlig.
const listeValg = new URLSearchParams(window.location.search).get('liste');

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

        // Markøren lagres på punktet, så filteret kan ta den bort fra kartet og legge den tilbake.
        const markor = L.marker([behov.breddegrad, behov.lengdegrad], { icon: lagKartikon('behov', behov.typeKode, fargeKlasse) })
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
            markor: markor,
            prioritet: behov.prioritet,      // "hoy", "moderat" eller "lav", brukes av filteret
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

        const markor = L.marker([ressurs.breddegrad, ressurs.lengdegrad], { icon: lagKartikon('ressurs', ressurs.typeKode, 'bp-ikon-ressurs') })
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
            markor: markor,
            // Samme ikon som på kartet, brukes i listen.
            form: 'ressurs',
            typeKode: ressurs.typeKode,
            fargeKlasse: 'bp-ikon-ressurs',
            hva: ressurs.tildelt ? 'Ressurs (tildelt)' : 'Ressurs',
            erTildelt: ressurs.tildelt,
            type: ressurs.type,
            sted: ressurs.adresse,
            beskrivelse: ressurs.beskrivelse
        });
    }
}

/** Skriver punktene som passer filteret inn i tabellen (listen). */
function visTabell() {
    // Bare punktene filteret viser (se filtrer() lenger ned).
    const punkter = punkterTilTabell.filter(punkt => punkt.synlig);

    // Viser antall punkter i overskriften på listen.
    document.getElementById('kart-antall').textContent = punkter.length;

    if (punkter.length === 0) {
        tabell.innerHTML = '<tr><td colspan="5" class="text-muted">Ingen punkter å vise.</td></tr>';
        return;
    }

    let rader = '';
    for (const punkt of punkter) {
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

// =========================================================================
// Filter (panelet til venstre på kartet)
// =========================================================================

const filterInnhold = document.getElementById('filter-innhold');

/**
 * Lager én avkrysningsboks med ikon, navn og antall.
 * data-* sier hva boksen styrer, og leses av filtrer().
 */
function lagFilterValg(dataAttributter, ikonHtml, tekst, antall, ekstraKlasse) {
    return '<label class="bp-filter-valg ' + (ekstraKlasse || '') + '">' +
        '<input type="checkbox" checked ' + dataAttributter + '>' +
        ikonHtml +
        '<span class="bp-filter-navn">' + trygg(tekst) + '</span>' +
        (antall === null ? '' : '<span class="bp-filter-antall">' + antall + '</span>') +
        '</label>';
}

/**
 * Lager innholdet i filterpanelet ut fra punktene som er hentet.
 * Typene og antallene regnes ut her, så nye typer kommer med av seg selv.
 */
function byggFilter() {
    let html = '';

    for (const form of ['behov', 'ressurs']) {
        const punkter = punkterTilTabell.filter(punkt => punkt.form === form);
        if (punkter.length === 0) {
            continue;   // f.eks. ingen ressurser: da vises ikke gruppen
        }

        // Teller hvor mange punkter det er av hver type, f.eks. { Nodstrom: { navn: "Nødstrøm (aggregat)", antall: 3 } }.
        const typer = {};
        for (const punkt of punkter) {
            if (!typer[punkt.typeKode]) {
                typer[punkt.typeKode] = { navn: punkt.type, antall: 0 };
            }
            typer[punkt.typeKode].antall++;
        }

        const erBehov = form === 'behov';
        // Gruppeikonet er den samme formen som i tegnforklaringen: rund prikk for behov, firkant for ressurser.
        const gruppeIkon = erBehov ? '<span class="bp-kart-prikk hoy"></span>' : '<span class="bp-kart-firkant"></span>';

        html += '<div class="bp-filter-gruppe">';
        html += lagFilterValg('data-gruppe-hoved="' + form + '"', gruppeIkon,
            erBehov ? 'Behov' : 'Ressurser', punkter.length, 'bp-filter-hoved');

        // Én boks per type, med samme ikon som på kartet
        for (const typeKode in typer) {
            const fargeKlasse = erBehov ? 'bp-ikon-hoy' : 'bp-ikon-ressurs';
            const ikon = '<span class="bp-kartikon ' + fargeKlasse + '">' + lagIkonSvg(form, typeKode, 22) + '</span>';
            html += lagFilterValg('data-gruppe="' + form + '" data-type="' + trygg(typeKode) + '"',
                ikon, typer[typeKode].navn, typer[typeKode].antall, 'bp-filter-type');
        }

        if (erBehov) {
            // Prioritet: tre små valg med fargeprikk
            html += '<div class="bp-filter-prioritet"><span class="bp-filter-undertittel">Prioritet</span>';
            for (const [kode, tekst] of [['hoy', 'Høy'], ['moderat', 'Moderat'], ['lav', 'Lav']]) {
                html += lagFilterValg('data-prioritet="' + kode + '"', '<span class="bp-kart-prikk ' + kode + '"></span>', tekst, null, '');
            }
            html += '</div>';
        } else {
            // Skjul ressurser som allerede er tildelt et behov (av som standard)
            html += '<label class="bp-filter-valg bp-filter-ekstra">' +
                '<input type="checkbox" id="filter-skjul-tildelte">' +
                '<span class="bp-filter-navn">Skjul tildelte</span></label>';
        }
        html += '</div>';
    }

    filterInnhold.innerHTML = html;
}

/** Krysser av (eller fjerner krysset) for en hel gruppe, f.eks. alle behov. */
function settGruppe(form, valgt) {
    filterInnhold.querySelectorAll('[data-gruppe="' + form + '"]').forEach(boks => { boks.checked = valgt; });
    oppdaterHovedboks(form);
}

/**
 * Hovedboksen (Behov / Ressurser) viser om alle, ingen eller noen av typene er valgt.
 * "indeterminate" er den halvveis-markeringen når bare noen er valgt.
 */
function oppdaterHovedboks(form) {
    const hoved = filterInnhold.querySelector('[data-gruppe-hoved="' + form + '"]');
    if (!hoved) {
        return;
    }
    const bokser = [...filterInnhold.querySelectorAll('[data-gruppe="' + form + '"]')];
    const antallValgt = bokser.filter(boks => boks.checked).length;
    hoved.checked = antallValgt === bokser.length;
    hoved.indeterminate = antallValgt > 0 && antallValgt < bokser.length;
}

/**
 * Går gjennom alle punktene og bestemmer om de skal vises, ut fra filteret.
 * Synlige punkter legges på kartet, de andre tas bort. Listen og "Treff" oppdateres.
 */
function filtrer() {
    // Hvilke typer som er valgt, f.eks. { "behov:Nodstrom": true, "ressurs:Aggregat": false }
    const valgteTyper = {};
    filterInnhold.querySelectorAll('[data-type]').forEach(boks => {
        valgteTyper[boks.dataset.gruppe + ':' + boks.dataset.type] = boks.checked;
    });

    // Hvilke prioriteter som er valgt, f.eks. { hoy: true, moderat: true, lav: false }
    const valgtePrioriteter = {};
    filterInnhold.querySelectorAll('[data-prioritet]').forEach(boks => {
        valgtePrioriteter[boks.dataset.prioritet] = boks.checked;
    });

    const skjulTildelteBoks = document.getElementById('filter-skjul-tildelte');
    const skjulTildelte = skjulTildelteBoks ? skjulTildelteBoks.checked : false;

    let antallSynlige = 0;
    for (const punkt of punkterTilTabell) {
        let synlig = valgteTyper[punkt.form + ':' + punkt.typeKode] === true;
        if (punkt.form === 'behov' && !valgtePrioriteter[punkt.prioritet]) {
            synlig = false;
        }
        if (punkt.form === 'ressurs' && skjulTildelte && punkt.erTildelt) {
            synlig = false;
        }

        punkt.synlig = synlig;
        if (synlig) {
            punkt.markor.addTo(kart);
            antallSynlige++;
        } else {
            kart.removeLayer(punkt.markor);
        }
    }

    document.getElementById('filter-treff').textContent = antallSynlige;

    // På Filter-knappen vises "(14 av 40)" når noe er filtrert bort, så man ser at filteret er i bruk.
    const totalt = punkterTilTabell.length;
    document.getElementById('filter-knapp-status').textContent =
        antallSynlige < totalt ? '(' + antallSynlige + ' av ' + totalt + ')' : '';

    visTabell();
}

// Når en boks i panelet endres: oppdater hovedboksene og filtrer på nytt.
filterInnhold.addEventListener('change', function (hendelse) {
    const boks = hendelse.target;
    if (boks.dataset.gruppeHoved) {
        // Hovedboksen styrer alle typene i gruppen
        settGruppe(boks.dataset.gruppeHoved, boks.checked);
    } else if (boks.dataset.gruppe) {
        oppdaterHovedboks(boks.dataset.gruppe);
    }
    filtrer();
});

// "Nullstill": alt vises igjen
document.getElementById('filter-nullstill').addEventListener('click', function () {
    filterInnhold.querySelectorAll('input[type="checkbox"]').forEach(boks => { boks.checked = true; });
    const skjulTildelte = document.getElementById('filter-skjul-tildelte');
    if (skjulTildelte) {
        skjulTildelte.checked = false;
    }
    oppdaterHovedboks('behov');
    oppdaterHovedboks('ressurs');
    filtrer();
});

// "Filter"-knappen åpner panelet, og krysset i panelet lukker det.
// Panelet er lukket når kartet åpnes, så kartet får hele plassen.
const filterPanel = document.getElementById('kart-filter');
const filterKnapp = document.getElementById('filter-apne');

function settFilterApent(apent) {
    filterPanel.classList.toggle('skjult', !apent);
    filterKnapp.hidden = apent;      // knappen skjules mens panelet er åpent
    filterKnapp.setAttribute('aria-expanded', apent ? 'true' : 'false');
}

filterKnapp.addEventListener('click', function () { settFilterApent(true); });
document.getElementById('filter-lukk').addEventListener('click', function () { settFilterApent(false); });

/** Starter alt sammen. Kjøres én gang når siden lastes. */
async function start() {
    try {
        await visBehov();
        if (kanSeRessurser) {
            await visRessurser();
        }
        byggFilter();

        // Kommer brukeren fra "Tilgjengelige ressurser" på oversikten, settes filteret
        // til bare ressurser som ikke er tildelt, og listen får en annen overskrift.
        if (listeValg === 'ressurser') {
            settGruppe('behov', false);
            document.getElementById('filter-skjul-tildelte').checked = true;
            document.getElementById('kart-liste-tittel-tekst').textContent = 'Tilgjengelige ressurser';
        }

        filtrer();

        // Åpner listen med en gang hvis adressen ber om det (?liste=...).
        // bootstrap.Offcanvas er Bootstrap sin kode for panelet, den samme som knappen "Vis liste" bruker.
        if (listeValg) {
            bootstrap.Offcanvas.getOrCreateInstance(document.getElementById('kart-liste-panel')).show();
        }
    } catch (feil) {
        console.error(feil);
        tabell.innerHTML = '<tr><td colspan="5" class="text-danger">Klarte ikke å hente punktene til kartet.</td></tr>';
    }
}

start();
