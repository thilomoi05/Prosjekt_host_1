// =========================================================================
// sokefelt.js
// Det som er felles for alle søkefeltene (Views/Shared/_Sokefelt.cshtml).
// Må lastes før skriptet som gjør selve søket på siden (tabellfilter.js, kart.js
// eller tildelsok.js), fordi de bruker funksjonene her.
//
// Hva filen gjør:
//  1. Gir funksjonene sokeord() og passerSok(), så alle sidene søker på samme måte:
//     store og små bokstaver er likt, og hvert ord må finnes et sted i teksten.
//     "aggregat lund" finner altså et aggregat i Lund.
//  2. Viser krysset i feltet når det står noe der. Krysset og Esc tømmer søket.
//  3. Fyller inn søket fra adressen, f.eks. /Behov?sok=lund, på samme måte som
//     filtrene kan settes i adressen (/Behov?status=Venter).
//  4. Hindrer at Enter i søkefeltet sender skjemaet på Tildel-siden.
// =========================================================================

/** Gjør teksten klar til sammenligning: små bokstaver og bare ett mellomrom mellom ordene. */
function normaliserSok(tekst) {
    return (tekst || '').toLocaleLowerCase('nb').replace(/\s+/g, ' ').trim();
}

/** Ordene i et søkefelt, f.eks. "Aggregat  Lund" gir ["aggregat", "lund"]. Tomt felt gir []. */
function sokeord(felt) {
    const tekst = normaliserSok(felt ? felt.value : '');
    return tekst === '' ? [] : tekst.split(' ');
}

/** true hvis alle ordene finnes i teksten. Et tomt søk passer alt. */
function passerSok(tekst, ord) {
    const normalisert = normaliserSok(tekst);
    return ord.every(etOrd => normalisert.includes(etOrd));
}

/** Tømmer feltet og sier fra til siden, så lista vises som før. */
function tomSokefelt(felt) {
    felt.value = '';
    felt.dispatchEvent(new Event('input', { bubbles: true }));
}

document.querySelectorAll('[data-sokefelt]').forEach(boks => {
    const felt = boks.querySelector('input');
    const tomKnapp = boks.querySelector('[data-sok-tom]');

    // Søk fra adressen, f.eks. /Behov?sok=lund
    const fraAdressen = new URLSearchParams(window.location.search).get('sok');
    if (fraAdressen && felt.value === '') {
        felt.value = fraAdressen;
    }

    // Krysset vises bare når det står noe i feltet.
    const visKryss = () => { tomKnapp.hidden = felt.value === ''; };
    felt.addEventListener('input', visKryss);
    visKryss();

    tomKnapp.addEventListener('click', () => {
        tomSokefelt(felt);
        felt.focus();
    });

    felt.addEventListener('keydown', hendelse => {
        // Enter skal ikke sende et skjema. Lista er allerede oppdatert mens man skrev.
        if (hendelse.key === 'Enter') {
            hendelse.preventDefault();
        }
        // Esc tømmer søket. stopPropagation gjør at lista under kartet ikke lukkes samtidig.
        if (hendelse.key === 'Escape' && felt.value !== '') {
            hendelse.preventDefault();
            hendelse.stopPropagation();
            tomSokefelt(felt);
        }
    });
});
