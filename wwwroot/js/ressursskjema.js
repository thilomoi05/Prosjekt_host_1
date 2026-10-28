// =========================================================================
// ressursskjema.js
// Kjører på siden "Tilby ressurs" (Views/Ressurs/Registrer.cshtml).
//
// Hva filen gjør:
//  1. Deler skjemaet i tre steg og viser ett om gangen (Neste / Forrige).
//  2. Sjekker feltene i steget man er på før man får gå videre, så man får
//     beskjed om feil med en gang og ikke først etter tre steg.
//  3. Setter inn ikonene på typeknappene og bytter eksempeltekst etter type.
//  4. Viser små forhåndsvisninger av bildene man har valgt.
//  5. Skjuler datoene når "Tilgjengelig nå, på ubestemt tid" er krysset av.
//  6. Lager oppsummeringen i steg 3.
// Skjemaet sendes til serveren som vanlig når man trykker "Registrer ressurs".
// =========================================================================

const skjema = document.getElementById('ressursskjema');
const seksjoner = [...skjema.querySelectorAll('.bp-steg-innhold')];
const fremdriftSteg = [...document.querySelectorAll('#fremdrift li')];
const knappForrige = document.getElementById('steg-forrige');
const knappNeste = document.getElementById('steg-neste');
const knappRegistrer = document.getElementById('steg-registrer');
const lenkeAvbryt = document.getElementById('steg-avbryt');

let gjeldendeSteg = 1;

// Vanligvis sjekker jQuery Validation et felt når man forlater det. Da kan en feilmelding
// forsvinne akkurat når man trykker "Neste", knappen flytter seg og klikket bommer.
// Derfor sjekkes feltene bare når man trykker "Neste" (og mens man skriver i et felt med feil).
// $(function () { ... }) venter til siden er ferdig lastet, så jQuery Validation er satt opp først.
$(function () {
    $(skjema).validate().settings.onfocusout = false;
});

/** Viser steget med nummeret "steg" og oppdaterer fremdriftslinjen og knappene. */
function visSteg(steg) {
    gjeldendeSteg = steg;

    seksjoner.forEach(seksjon => { seksjon.hidden = Number(seksjon.dataset.steg) !== steg; });

    fremdriftSteg.forEach(li => {
        const nummer = Number(li.dataset.steg);
        li.classList.toggle('aktiv', nummer === steg);
        li.classList.toggle('ferdig', nummer < steg);
    });

    knappForrige.hidden = steg === 1;
    lenkeAvbryt.hidden = steg !== 1;           // "Avbryt" bare på første steg, ellers "Forrige"
    knappNeste.hidden = steg === 3;
    knappRegistrer.hidden = steg !== 3;

    // Kartet ble laget mens steg 2 var skjult, og vet derfor ikke hvor stort det er.
    // invalidateSize() ber Leaflet måle på nytt (velgerKart kommer fra posisjonsvelger.js).
    if (steg === 2) {
        velgerKart.invalidateSize();
    }

    if (steg === 3) {
        lagOppsummering();
    }

    window.scrollTo({ top: 0 });
}

/**
 * Sjekker feltene i det synlige steget. Returnerer true hvis alt er i orden.
 * .valid() kommer fra jQuery Validation og bruker reglene fra RessursViewModel
 * ([Required], [Phone] osv.), og viser feilmeldingen under feltet.
 */
function stegetErGyldig(steg) {
    const seksjon = seksjoner[steg - 1];
    let gyldig = true;

    $(seksjon).find('input:not([type=hidden]):not([type=file]), select, textarea').each(function () {
        if (!$(this).valid()) {
            gyldig = false;
        }
    });

    // Bilder og plassering har ingen regler jQuery Validation kjenner til, så de sjekkes her.
    if (steg === 1) {
        const bildefelt = document.getElementById('Bilder');
        const bilderFeil = document.getElementById('bilder-feil');
        if (bildefelt.files.length === 0) {
            bilderFeil.textContent = 'Du må legge ved minst ett bilde av ressursen.';
            gyldig = false;
        } else if (bildefelt.files.length > 5) {
            bilderFeil.textContent = 'Du kan legge ved maks 5 bilder.';
            gyldig = false;
        } else {
            bilderFeil.textContent = '';
        }
    }

    if (steg === 2) {
        const plasseringFeil = document.getElementById('plassering-feil');
        if (document.getElementById('Breddegrad').value === '') {
            plasseringFeil.textContent = 'Trykk «Finn på kartet», eller klikk i kartet der ressursen er.';
            gyldig = false;
        } else {
            plasseringFeil.textContent = '';
        }
    }

    return gyldig;
}

knappNeste.addEventListener('click', function () {
    if (stegetErGyldig(gjeldendeSteg)) {
        visSteg(gjeldendeSteg + 1);
    }
});

knappForrige.addEventListener('click', function () {
    visSteg(gjeldendeSteg - 1);
});

// Trykker man Enter i et felt før siste steg, går vi til neste steg i stedet for å sende skjemaet.
skjema.addEventListener('keydown', function (hendelse) {
    if (hendelse.key === 'Enter' && hendelse.target.tagName === 'INPUT' && gjeldendeSteg < 3) {
        hendelse.preventDefault();
    }
});

// ---------- Typeknappene: ikon og eksempeltekst ----------

// Samme ikon som på kartet (lagIkonSvg fra kartikoner.js).
document.querySelectorAll('.bp-type-knapp [data-ikon-type]').forEach(element => {
    element.innerHTML = lagIkonSvg('ressurs', element.dataset.ikonType, 36);
});

// Eksempeltekst i beskrivelsesfeltet for hver type, så man vet hva som er nyttig å skrive.
const eksempelTekster = {
    Aggregat: 'F.eks. 50 kW dieselaggregat, kan gå i 8 timer på full tank.',
    Drivstoff: 'F.eks. 1000 liter diesel på tank, kan hentes med egen henger.',
    Ups: 'F.eks. UPS på 3 kW som holder en PC og en router i gang i 2 timer.',
    Transport: 'F.eks. lastebil med plass til 10 paller, sjåfør kan stille.',
    Kommunikasjonsutstyr: 'F.eks. 4 jaktradioer og en satellittelefon.',
    Annet: 'Beskriv hva det er, hvor mye du har, og hva det kan brukes til.'
};

const beskrivelseFelt = document.getElementById('BeskrivelseAvKapasitet');
skjema.querySelectorAll('input[name="Type"]').forEach(radio => {
    radio.addEventListener('change', function () {
        beskrivelseFelt.placeholder = eksempelTekster[radio.value];
    });
});

// ---------- Forhåndsvisning av bilder ----------

const bildefelt = document.getElementById('Bilder');
const forhandsvisning = document.getElementById('bilde-forhandsvisning');

bildefelt.addEventListener('change', function () {
    forhandsvisning.innerHTML = '';
    for (const fil of bildefelt.files) {
        // URL.createObjectURL lager en midlertidig adresse til bildet på brukerens egen maskin,
        // så det kan vises før skjemaet er sendt.
        const bilde = document.createElement('img');
        bilde.src = URL.createObjectURL(fil);
        bilde.alt = fil.name;
        forhandsvisning.appendChild(bilde);
    }
    document.getElementById('bilder-feil').textContent = '';
});

// ---------- "Tilgjengelig nå, på ubestemt tid" ----------

const tilgjengeligNa = document.getElementById('tilgjengelig-na');
const datoer = document.getElementById('datoer');

tilgjengeligNa.addEventListener('change', function () {
    datoer.hidden = tilgjengeligNa.checked;
    if (tilgjengeligNa.checked) {
        // Tomme datoer betyr "nå, på ubestemt tid".
        document.getElementById('TilgjengeligFra').value = '';
        document.getElementById('TilgjengeligTil').value = '';
    }
});

// ---------- Oppsummering i steg 3 ----------

/** Gjør tekst trygg å sette inn som HTML (samme funksjon som i kart.js). */
function trygg(tekst) {
    const div = document.createElement('div');
    div.textContent = tekst ?? '';
    return div.innerHTML;
}

/** Skriver det brukeren har fylt inn i oppsummeringen, med "Endre"-knapp til riktig steg. */
function lagOppsummering() {
    const valgtType = skjema.querySelector('input[name="Type"]:checked');
    const typeTekst = valgtType ? valgtType.closest('label').textContent.trim() : '';

    const fra = document.getElementById('TilgjengeligFra').value;
    const til = document.getElementById('TilgjengeligTil').value;
    const tilgjengelig = tilgjengeligNa.checked || (!fra && !til)
        ? 'Nå, på ubestemt tid'
        : (fra || 'Nå') + ' til ' + (til || 'ubestemt tid');

    const rader = [
        ['Type', typeTekst, 1],
        ['Beskrivelse', beskrivelseFelt.value, 1],
        ['Bilder', bildefelt.files.length + ' valgt', 1],
        ['Adresse', document.getElementById('Adresse').value, 2],
        ['Tilgjengelig', tilgjengelig, 3]
    ];

    document.getElementById('oppsummering').innerHTML = rader.map(([etikett, verdi, steg]) =>
        '<dt>' + etikett + '</dt>' +
        '<dd>' + trygg(verdi) +
        ' <button type="button" class="bp-oppsummering-endre" data-til-steg="' + steg + '">Endre</button></dd>'
    ).join('');
}

// "Endre" i oppsummeringen går tilbake til steget feltet hører til.
document.getElementById('oppsummering').addEventListener('click', function (hendelse) {
    const knapp = hendelse.target.closest('[data-til-steg]');
    if (knapp) {
        visSteg(Number(knapp.dataset.tilSteg));
    }
});

// ---------- Oppstart ----------

// Kom skjemaet tilbake fra serveren med feil, starter vi på det første steget som har en feil.
// Feilmeldingene står i elementer med klassen "field-validation-error".
function forsteStegMedFeil() {
    for (const seksjon of seksjoner) {
        const feil = [...seksjon.querySelectorAll('.field-validation-error')].some(el => el.textContent.trim() !== '');
        if (feil) {
            return Number(seksjon.dataset.steg);
        }
    }
    return 1;
}

visSteg(forsteStegMedFeil());
