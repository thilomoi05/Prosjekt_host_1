// =========================================================================
// kart.js
// Kjører på kartsiden (Views/Kart/Index.cshtml).
//
// Hva filen gjør:
//  1. Lager kartet (med lagKart fra kartverket.js).
//  2. Henter behov fra serveren (/Kart/Behov) og tegner dem som kartnåler.
//  3. Henter ressurser (/Kart/Ressurser) og tegner dem som firkanter,
//     men bare hvis brukeren har lov (serveren sjekker det også).
//  4. Lager filterpanelet til venstre (typer, prioritet, status).
//  5. Viser bare punktene som passer filteret, både på kartet og i listen.
//  6. Lager filtermenyene i kolonneoverskriftene i listen. De er koblet til
//     boksene i filterpanelet, så kartet og listen alltid viser det samme.
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
 * Lager lenken "Se detaljer". Brukes både i boblen på kartet og i listen.
 * form er 'behov' eller 'ressurs' og bestemmer hvilken detaljside lenken går til:
 * /Behov/Detaljer/5 eller /Ressurs/Detaljer/7.
 * ekstraKlasser er ekstra CSS-klasser, f.eks. 'w-100 mt-2' for full bredde i boblen.
 */
function seDetaljerKnapp(form, id, ekstraKlasser = '') {
    const adresse = (form === 'ressurs' ? '/Ressurs/Detaljer/' : '/Behov/Detaljer/') + encodeURIComponent(id);
    return '<a href="' + adresse + '" class="btn btn-sm btn-outline-primary bp-kart-detaljer ' + ekstraKlasser + '">' +
        'Se detaljer &rarr;</a>';
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
 *   id                           brukes av "Se detaljer"-lenken (sammen med form)
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
        seDetaljerKnapp(punkt.form, punkt.id, 'w-100 mt-3') +
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
            prioritetTekst: behov.prioritetTekst,
            status: behov.status,            // "Venter" eller "Tildelt", brukes av filteret
            // Samme ikon som på kartet, brukes i listen.
            form: 'behov',
            typeKode: behov.typeKode,
            fargeKlasse: fargeKlasse,
            hva: 'Behov',
            type: behov.type,
            sted: behov.omrade
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
            hva: 'Ressurs',
            // "Tilgjengelig" eller "Tildelt", brukes av filteret og vises i listen.
            status: ressurs.tildelt ? 'Tildelt' : 'Tilgjengelig',
            type: ressurs.type,
            sted: ressurs.adresse
        });
    }
}

/** Skriver punktene som passer filteret inn i tabellen (listen). */
function visTabell() {
    // Bare punktene filteret viser (se filtrer() lenger ned).
    const punkter = punkterTilTabell.filter(punkt => punkt.synlig);

    // "Viser 9 av 40" over listen, og "Nullstill filter" når noe er filtrert bort.
    const totalt = punkterTilTabell.length;
    document.getElementById('kart-antall').textContent = punkter.length < totalt
        ? 'Viser ' + punkter.length + ' av ' + totalt
        : 'Viser alle ' + totalt;
    document.getElementById('liste-nullstill').hidden = punkter.length === totalt;

    if (punkter.length === 0) {
        tabell.innerHTML = '<tr><td colspan="6" class="text-muted">Ingen punkter passer filteret. Trykk «Nullstill filter» for å se alle.</td></tr>';
        return;
    }

    let rader = '';
    for (const punkt of punkter) {
        // Status vises som et merke, samme farger som på Behov-siden: gul = venter/tilgjengelig, blå = tildelt.
        const statusKlasse = punkt.status === 'Tildelt' ? 'bp-badge-tildelt' : 'bp-badge-venter';
        // Bare behov har prioritet. Ressurser får en strek.
        const prioritet = punkt.form === 'behov'
            ? '<span class="bp-prioritet-' + punkt.prioritet + '">' + trygg(punkt.prioritetTekst) + '</span>'
            : '<span class="text-muted">&ndash;</span>';

        rader += '<tr>' +
            // Ikonet foran teksten (litt mindre enn på kartet), så listen og kartet ser like ut.
            '<td><span class="bp-kartikon ' + punkt.fargeKlasse + '">' +
                lagIkonSvg(punkt.form, punkt.typeKode, 24) + '</span> ' + trygg(punkt.hva) + '</td>' +
            '<td>' + trygg(punkt.type) + '</td>' +
            '<td>' + trygg(punkt.sted) + '</td>' +
            '<td>' + prioritet + '</td>' +
            '<td><span class="bp-badge ' + statusKlasse + '">' + trygg(punkt.status) + '</span></td>' +
            '<td class="text-end">' + seDetaljerKnapp(punkt.form, punkt.id, 'text-nowrap') + '</td>' +
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
        }

        // Status: behov venter eller er tildelt, ressurser er tilgjengelige eller tildelt.
        // data-status er f.eks. "ressurs:Tildelt". Å fjerne det krysset skjuler tildelte ressurser.
        const statuser = erBehov ? ['Venter', 'Tildelt'] : ['Tilgjengelig', 'Tildelt'];
        html += '<div class="bp-filter-prioritet"><span class="bp-filter-undertittel">Status</span>';
        for (const status of statuser) {
            html += lagFilterValg('data-status="' + form + ':' + status + '"', '', status, null, '');
        }
        html += '</div>';

        html += '</div>';
    }

    filterInnhold.innerHTML = html;

    // Hver boks får en id (filter-boks-0, filter-boks-1, ...), så boksene i listen kan peke på dem.
    filterInnhold.querySelectorAll('input').forEach((boks, nummer) => { boks.id = 'filter-boks-' + nummer; });
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

    // Hvilke statuser som er valgt, f.eks. { "behov:Venter": true, "ressurs:Tildelt": false }
    const valgteStatuser = {};
    filterInnhold.querySelectorAll('[data-status]').forEach(boks => {
        valgteStatuser[boks.dataset.status] = boks.checked;
    });

    let antallSynlige = 0;
    for (const punkt of punkterTilTabell) {
        let synlig = valgteTyper[punkt.form + ':' + punkt.typeKode] === true;
        if (punkt.form === 'behov' && !valgtePrioriteter[punkt.prioritet]) {
            synlig = false;
        }
        if (!valgteStatuser[punkt.form + ':' + punkt.status]) {
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
    oppdaterListemenyer();
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

// =========================================================================
// Filtermenyene i kolonneoverskriftene i listen
//
// Menyene har ingen egen logikk. Hver avkrysningsboks i en meny er koblet til
// en boks i filterpanelet (data-styrer="filter-boks-3"). Krysser man av i listen,
// krysses boksen i panelet av, og filtrer() kjører som vanlig. Etterpå kopierer
// oppdaterListemenyer() krysset tilbake fra panelet. Én kilde, to visninger.
// =========================================================================

// Hvilke bokser i panelet hver kolonne viser. Hver del er [overskrift, CSS-velger].
// Overskriften skiller behov og ressurser når kolonnen har begge (tom = ingen overskrift).
const LISTEKOLONNER = [
    { th: 'liste-kolonne-hva', tittel: 'Hva', deler: [['', '[data-gruppe-hoved]']] },
    { th: 'liste-kolonne-type', tittel: 'Type', deler: [['Behov', '[data-gruppe="behov"]'], ['Ressurser', '[data-gruppe="ressurs"]']] },
    { th: 'liste-kolonne-prioritet', tittel: 'Prioritet', deler: [['', '[data-prioritet]']] },
    { th: 'liste-kolonne-status', tittel: 'Status', deler: [['Behov', '[data-status^="behov:"]'], ['Ressurser', '[data-status^="ressurs:"]']] }
];

/** Lager menyene i kolonneoverskriftene. Kjøres én gang, etter byggFilter(). */
function byggListemenyer() {
    for (const kolonne of LISTEKOLONNER) {
        let valgHtml = '';
        for (const [overskrift, velger] of kolonne.deler) {
            const panelBokser = filterInnhold.querySelectorAll(velger);
            if (panelBokser.length === 0) {
                continue;   // f.eks. ingen ressurser
            }
            if (overskrift) {
                valgHtml += '<span class="bp-kolonne-meny-overskrift">' + overskrift + '</span>';
            }
            for (const panelBoks of panelBokser) {
                // Henter ikonet og navnet fra samme rad i panelet, så de ser like ut.
                const rad = panelBoks.closest('label');
                const ikon = rad.querySelector('.bp-kartikon, .bp-kart-prikk, .bp-kart-firkant');
                valgHtml += '<label class="bp-filter-valg">' +
                    '<input type="checkbox" checked data-styrer="' + panelBoks.id + '">' +
                    (ikon ? ikon.outerHTML : '') +
                    '<span class="bp-filter-navn">' + trygg(rad.querySelector('.bp-filter-navn').textContent) + '</span>' +
                    '</label>';
            }
        }

        // Samme oppbygning som _Filtermeny.cshtml på Behov-siden (knapp med pil + meny).
        // data-bs-auto-close="outside": menyen lukker seg ikke når man krysser av.
        document.getElementById(kolonne.th).innerHTML =
            '<div class="dropdown">' +
                '<button type="button" class="bp-kolonne-knapp" data-bs-toggle="dropdown" data-bs-auto-close="outside" aria-expanded="false">' +
                    kolonne.tittel + ' <span class="bp-kolonne-teller"></span>' +
                    '<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><polyline points="6 9 12 15 18 9" /></svg>' +
                '</button>' +
                '<div class="dropdown-menu bp-kolonne-meny">' + valgHtml +
                    '<div class="bp-kolonne-meny-fot">' +
                        '<button type="button" data-velg="alle">Alle</button>' +
                        '<button type="button" data-velg="ingen">Ingen</button>' +
                    '</div>' +
                '</div>' +
            '</div>';
    }
}

/** Kopierer kryssene fra panelet til menyene i listen, og oppdaterer tellerne på knappene. */
function oppdaterListemenyer() {
    for (const kolonne of LISTEKOLONNER) {
        const th = document.getElementById(kolonne.th);
        const listeBokser = [...th.querySelectorAll('[data-styrer]')];
        if (listeBokser.length === 0) {
            continue;   // menyene er ikke laget ennå
        }

        for (const listeBoks of listeBokser) {
            const panelBoks = document.getElementById(listeBoks.dataset.styrer);
            listeBoks.checked = panelBoks.checked;
            listeBoks.indeterminate = panelBoks.indeterminate;   // halvveis (f.eks. bare noen behovstyper)
        }

        // Kolonner med filter i bruk får understrek og antall valgte, f.eks. "Type (3)".
        const antallValgt = listeBokser.filter(boks => boks.checked && !boks.indeterminate).length;
        const erFiltrert = antallValgt < listeBokser.length;
        th.querySelector('.bp-kolonne-knapp').classList.toggle('aktiv', erFiltrert);
        th.querySelector('.bp-kolonne-teller').textContent = erFiltrert ? '(' + antallValgt + ')' : '';
    }
}

/** Setter en boks i panelet til samme kryss som boksen i listen, og lar panelet gjøre resten. */
function styrPanelboks(listeBoks) {
    const panelBoks = document.getElementById(listeBoks.dataset.styrer);
    panelBoks.checked = listeBoks.checked;
    // Later som brukeren klikket i panelet, så den vanlige koden kjører (hovedbokser + filtrer()).
    panelBoks.dispatchEvent(new Event('change', { bubbles: true }));
}

const listeHode = document.getElementById('kart-liste-hode');

// En boks i en meny i listen endres
listeHode.addEventListener('change', function (hendelse) {
    if (hendelse.target.dataset.styrer) {
        styrPanelboks(hendelse.target);
    }
});

// "Alle" og "Ingen" nederst i hver meny
listeHode.addEventListener('click', function (hendelse) {
    const knapp = hendelse.target.closest('[data-velg]');
    if (!knapp) {
        return;
    }
    const meny = knapp.closest('.bp-kolonne-meny');
    meny.querySelectorAll('[data-styrer]').forEach(listeBoks => {
        listeBoks.checked = knapp.dataset.velg === 'alle';
        styrPanelboks(listeBoks);
    });
});

// "Nullstill filter" over listen gjør det samme som "Nullstill" i panelet.
document.getElementById('liste-nullstill').addEventListener('click', function () {
    document.getElementById('filter-nullstill').click();
});

/** Starter alt sammen. Kjøres én gang når siden lastes. */
async function start() {
    try {
        await visBehov();
        if (kanSeRessurser) {
            await visRessurser();
        }
        byggFilter();
        byggListemenyer();

        // Kommer brukeren fra "Tilgjengelige ressurser" på oversikten, settes filteret
        // til bare ressurser som ikke er tildelt, og listen får en annen overskrift.
        if (listeValg === 'ressurser') {
            settGruppe('behov', false);
            filterInnhold.querySelector('[data-status="ressurs:Tildelt"]').checked = false;
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
        tabell.innerHTML = '<tr><td colspan="6" class="text-danger">Klarte ikke å hente punktene til kartet.</td></tr>';
    }
}

start();
