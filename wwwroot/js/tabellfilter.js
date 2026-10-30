// =========================================================================
// tabellfilter.js
// Filter og datosortering for tabeller med filtermenyer i kolonneoverskriftene.
// Brukes av Behov-siden (Views/Behov/Index.cshtml) og Tildelinger-siden
// (Views/Tildeling/Index.cshtml), så filterlogikken bare finnes ett sted.
//
// Hva filen gjør:
//  1. Filtrerer radene ut fra avkrysningene i menyene (Views/Shared/_Filtermeny.cshtml).
//  2. Sorterer på dato, nyeste eller eldste først (Views/Shared/_Datosortering.cshtml).
//  3. Viser "Viser 8 av 12 behov", tellere på kolonnene og "Nullstill filter".
//  4. Leser filter fra adressen, f.eks. /Behov?status=Venter, så andre sider
//     kan lenke rett til en filtrert liste.
//  5. Setter inn kartikonene der det står data-ikon-type.
//
// Siden må ha:
//  - en tabell med id="filtertabell" og data-enhet="behov" (ordet i "Viser 8 av 12 behov")
//  - rader med data-rad og én data-* per filter, f.eks. data-prioritet="hoy", og data-dato
//  - id="tabell-antall", id="tabell-nullstill" og en tom-rad med id="tabell-tom"
// =========================================================================

const filtertabell = document.getElementById('filtertabell');
const tabellKropp = filtertabell.querySelector('tbody');
const rader = [...tabellKropp.querySelectorAll('tr[data-rad]')];
const tomRad = document.getElementById('tabell-tom');
const menyer = [...document.querySelectorAll('.bp-kolonne-meny[data-filter]')];
const nullstillKnapp = document.getElementById('tabell-nullstill');
const enhet = filtertabell.dataset.enhet;   // f.eks. "behov" eller "tildelinger"

// Setter inn kartikonene. data-ikon-form="ressurs" gir firkant, ellers nål (behov).
// Ikonene i menyene er mindre (data-ikon-storrelse="22").
document.querySelectorAll('[data-ikon-type]').forEach(element => {
    const form = element.dataset.ikonForm || 'behov';
    const storrelse = Number(element.dataset.ikonStorrelse || 28);
    element.innerHTML = lagIkonSvg(form, element.dataset.ikonType, storrelse);
});

// Husker den opprinnelige rekkefølgen, så sorteringen er stabil når to rader har samme dato.
rader.forEach((rad, indeks) => { rad.dataset.indeks = indeks; });

/** Hvilke verdier som er krysset av i menyen for et filter, f.eks. ["hoy", "moderat"]. */
function valgteVerdier(meny) {
    return [...meny.querySelectorAll('input[type="checkbox"]:checked')].map(boks => boks.value);
}

/** Viser bare radene som passer alle filtrene, og oppdaterer tekstene. */
function filtrer() {
    let antallSynlige = 0;

    for (const rad of rader) {
        // En rad vises hvis verdien dens er krysset av i hver eneste meny.
        const passer = menyer.every(meny => valgteVerdier(meny).includes(rad.dataset[meny.dataset.filter]));
        rad.hidden = !passer;
        if (passer) {
            antallSynlige++;
        }
    }

    tomRad.hidden = antallSynlige > 0;

    const erFiltrert = antallSynlige < rader.length;
    document.getElementById('tabell-antall').textContent = erFiltrert
        ? 'Viser ' + antallSynlige + ' av ' + rader.length + ' ' + enhet
        : 'Viser alle ' + rader.length + ' ' + enhet;
    nullstillKnapp.hidden = !erFiltrert;

    // Kolonner med filter i bruk får uthevet knapp og antall valgte, f.eks. "Område (3)".
    for (const meny of menyer) {
        const alle = meny.querySelectorAll('input[type="checkbox"]').length;
        const valgt = valgteVerdier(meny).length;
        const knapp = document.querySelector('[data-knapp-for="' + meny.dataset.filter + '"]');
        knapp.classList.toggle('aktiv', valgt < alle);
        knapp.querySelector('.bp-kolonne-teller').textContent = valgt < alle ? '(' + valgt + ')' : '';
    }
}

/** Sorterer radene på dato. Datoene står som "2026-09-02", så de kan sammenlignes som tekst. */
function sorter() {
    const nyestForst = document.querySelector('input[name="dato-sortering"]:checked').value === 'nyest';

    const sorterte = [...rader].sort((a, b) => {
        if (a.dataset.dato !== b.dataset.dato) {
            const forst = a.dataset.dato < b.dataset.dato ? -1 : 1;
            return nyestForst ? -forst : forst;
        }
        return a.dataset.indeks - b.dataset.indeks;   // samme dato: behold opprinnelig rekkefølge
    });

    // appendChild flytter en rad som allerede finnes, så radene havner i ny rekkefølge.
    sorterte.forEach(rad => tabellKropp.appendChild(rad));
    tabellKropp.appendChild(tomRad);   // "Ingen passer filteret" står alltid nederst

    document.getElementById('dato-retning').textContent = nyestForst ? '(nyeste først)' : '(eldste først)';
}

// Når en avkrysning endres i en meny: filtrer på nytt.
menyer.forEach(meny => meny.addEventListener('change', filtrer));

// "Alle" og "Ingen" nederst i hver meny.
menyer.forEach(meny => meny.addEventListener('click', function (hendelse) {
    const knapp = hendelse.target.closest('[data-velg]');
    if (!knapp) {
        return;
    }
    const velgAlle = knapp.dataset.velg === 'alle';
    meny.querySelectorAll('input[type="checkbox"]').forEach(boks => { boks.checked = velgAlle; });
    filtrer();
}));

// Datosortering
document.getElementById('dato-meny').addEventListener('change', sorter);

// "Nullstill filter": alt krysses av igjen.
nullstillKnapp.addEventListener('click', function () {
    document.querySelectorAll('.bp-kolonne-meny[data-filter] input[type="checkbox"]').forEach(boks => { boks.checked = true; });
    filtrer();
});

// Filter fra adressen, f.eks. /Behov?status=Venter eller /Behov?prioritet=hoy,moderat.
// Da krysses bare de verdiene av i den menyen.
const parametre = new URLSearchParams(window.location.search);
for (const meny of menyer) {
    const fraAdressen = parametre.get(meny.dataset.filter);
    if (fraAdressen) {
        const verdier = fraAdressen.split(',');
        meny.querySelectorAll('input[type="checkbox"]').forEach(boks => { boks.checked = verdier.includes(boks.value); });
    }
}

sorter();
filtrer();
