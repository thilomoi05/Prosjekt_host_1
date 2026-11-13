// =========================================================================
// tabellfilter.js
// Filter og datosortering for tabeller med filtermenyer i kolonneoverskriftene.
// Brukes av Behov-siden (Views/Behov/Index.cshtml), Tildelinger-siden
// (Views/Tildeling/Index.cshtml) og ressurslista (Views/Ressurs/Index.cshtml),
// så filterlogikken bare finnes ett sted.
//
// Hva filen gjør:
//  1. Filtrerer radene ut fra avkrysningene i menyene (Views/Shared/_Filtermeny.cshtml).
//  2. Sorterer på dato, nyeste eller eldste først (Views/Shared/_Datosortering.cshtml).
//     Datosorteringen er valgfri: har siden ingen id="dato-meny", står radene i rekkefølgen
//     fra serveren. data-dato-start="eldst" på tabellen gjør at den starter med eldste først.
//     Har radene data-gruppe (et tall), sorteres de først på den og så på dato innenfor hver
//     gruppe. Ressurslista bruker det til å ha tilgjengelige øverst og avviste nederst.
//  3. Viser "Viser 8 av 12 behov", tellere på kolonnene og "Nullstill filter".
//  4. Leser filter fra adressen, f.eks. /Behov?status=Venter, så andre sider
//     kan lenke rett til en filtrert liste.
//  5. Setter inn kartikonene der det står data-ikon-type.
//  6. Søker i radene mens man skriver i søkefeltet (Views/Shared/_Sokefelt.cshtml).
//     Søket treffer teksten i raden og det som står i data-sok (tekst som ikke vises
//     i tabellen, f.eks. beskrivelsen). Søket og filtrene virker sammen: en rad vises
//     bare hvis den passer begge.
//
// Siden må ha:
//  - en tabell med id="filtertabell" og data-enhet="behov" (ordet i "Viser 8 av 12 behov")
//  - rader med data-rad og én data-* per filter, f.eks. data-prioritet="hoy"
//  - id="tabell-antall", id="tabell-nullstill" og en tom-rad med id="tabell-tom"
// Siden kan ha:
//  - data-dato på radene og datosortering (id="dato-meny"), se punkt 2
//  - data-sok på radene og et søkefelt med id="tabell-sok". Da må sokefelt.js lastes før denne filen
// =========================================================================

const filtertabell = document.getElementById('filtertabell');
const tabellKropp = filtertabell.querySelector('tbody');
const rader = [...tabellKropp.querySelectorAll('tr[data-rad]')];
const tomRad = document.getElementById('tabell-tom');
const menyer = [...document.querySelectorAll('.bp-kolonne-meny[data-filter]')];
const nullstillKnapp = document.getElementById('tabell-nullstill');
const enhet = filtertabell.dataset.enhet;   // f.eks. "behov" eller "tildelinger"
const sokefelt = document.getElementById('tabell-sok');   // null hvis siden ikke har søkefelt

// Setter inn kartikonene. data-ikon-form="ressurs" gir firkant, ellers nål (behov).
// Ikonene i menyene er mindre (data-ikon-storrelse="22").
document.querySelectorAll('[data-ikon-type]').forEach(element => {
    const form = element.dataset.ikonForm || 'behov';
    const storrelse = Number(element.dataset.ikonStorrelse || 28);
    element.innerHTML = lagIkonSvg(form, element.dataset.ikonType, storrelse);
});

// Husker den opprinnelige rekkefølgen, så sorteringen er stabil når to rader har samme dato.
rader.forEach((rad, indeks) => { rad.dataset.indeks = indeks; });

// Teksten søket leter i for hver rad: det som står i cellene, pluss data-sok.
// Celler med en knapp (f.eks. "Endre ressursen") hoppes over, ellers ville "endre" treffe alle radene.
// Lages én gang, så søket går fort mens man skriver.
const sokeTekster = new Map(rader.map(rad => {
    const celler = [...rad.cells].filter(celle => !celle.querySelector('.btn'));
    return [rad, celler.map(celle => celle.textContent).join(' ') + ' ' + (rad.dataset.sok || '')];
}));

/** Hvilke verdier som er krysset av i menyen for et filter, f.eks. ["hoy", "moderat"]. */
function valgteVerdier(meny) {
    return [...meny.querySelectorAll('input[type="checkbox"]:checked')].map(boks => boks.value);
}

/** Viser bare radene som passer alle filtrene og søket, og oppdaterer tekstene. */
function filtrer() {
    let antallSynlige = 0;
    const ord = sokeord(sokefelt);   // [] når søkefeltet er tomt eller mangler

    for (const rad of rader) {
        // En rad vises hvis verdien dens er krysset av i hver eneste meny, og den passer søket.
        const passer = menyer.every(meny => valgteVerdier(meny).includes(rad.dataset[meny.dataset.filter]))
            && passerSok(sokeTekster.get(rad), ord);
        rad.hidden = !passer;
        if (passer) {
            antallSynlige++;
        }
    }

    tomRad.hidden = antallSynlige > 0;

    // "Nullstill filter" vises også når bare søket skjuler rader, for den tømmer søket også.
    const erFiltrert = antallSynlige < rader.length || ord.length > 0;
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

/** Sorterer radene på dato (og først på data-gruppe hvis radene har det).
 *  Datoene står som "2026-09-02", så de kan sammenlignes som tekst. */
function sorter() {
    // Ressurslista for private aktører har ingen datokolonne. Da står radene i rekkefølgen fra serveren.
    if (!document.getElementById('dato-meny')) {
        return;
    }
    const nyestForst = document.querySelector('input[name="dato-sortering"]:checked').value === 'nyest';

    const sorterte = [...rader].sort((a, b) => {
        // Rader uten data-gruppe (Behov og Tildelinger) får 0, så for dem er det bare datoen som teller.
        const gruppeForskjell = Number(a.dataset.gruppe || 0) - Number(b.dataset.gruppe || 0);
        if (gruppeForskjell !== 0) {
            return gruppeForskjell;
        }
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
document.getElementById('dato-meny')?.addEventListener('change', sorter);

// "Nullstill filter": alt krysses av igjen, og søket tømmes.
nullstillKnapp.addEventListener('click', function () {
    document.querySelectorAll('.bp-kolonne-meny[data-filter] input[type="checkbox"]').forEach(boks => { boks.checked = true; });
    if (sokefelt) {
        tomSokefelt(sokefelt);   // sier fra med en input-hendelse, så filtrer() kjører
    }
    filtrer();
});

// Søket: lista oppdateres for hver bokstav man skriver.
sokefelt?.addEventListener('input', filtrer);

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

// Tabellen kan be om å starte med eldste først, f.eks. ressurslista (data-dato-start="eldst").
if (filtertabell.dataset.datoStart === 'eldst') {
    const eldst = document.querySelector('input[name="dato-sortering"][value="eldst"]');
    if (eldst) {
        eldst.checked = true;
    }
}

sorter();
filtrer();
