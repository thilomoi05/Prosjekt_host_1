// =========================================================================
// kartikoner.js
// Ikonene som vises på kartsiden (lastes før kart.js).
//
// Hvert ikon består av to deler:
//  1. En FORM som viser om punktet er et behov eller en ressurs:
//       - behov   = rund kartnål ("her trengs noe")
//       - ressurs = avrundet firkant ("her finnes noe")
//  2. Et hvitt SYMBOL inni formen som viser hvilken type det er (lyn, termometer ...).
//
// Fargen settes med CSS (se "Kartikoner" i wwwroot/css/site.css). SVG-ene bruker
// fill="currentColor", som betyr "bruk tekstfargen fra CSS". Derfor kan vi
// bytte farge uten å endre ikonene.
//
// Symbolene er laget med ChatGPT (se KI-redegjørelsen) og justert av oss.
// =========================================================================

// De to formene. Begge er tegnet i et 24 x 24-rutenett (viewBox).
const KART_FORMER = {
    behov: '<path fill="currentColor" stroke="white" stroke-width="2" stroke-linejoin="round" d="M12 1.5C6.2 1.5 2 5.8 2 11.1c0 6.2 10 11.4 10 11.4s10-5.2 10-11.4C22 5.8 17.8 1.5 12 1.5z"/>',
    ressurs: '<rect x="1.5" y="1.5" width="21" height="21" rx="4" fill="currentColor" stroke="white" stroke-width="2"/>'
};

// Symbolene, ett per type.
const KART_SYMBOLER = {
    lyn: '<path fill="white" d="M13.5 2.5 5.5 13h6l-1 8.5L19 10.5h-6z"/>',
    drivstoff: '<path fill="white" fill-rule="evenodd" d="M5 3h10a2 2 0 0 1 2 2v15H3V5a2 2 0 0 1 2-2zm1 3v6h8V6z"/><path fill="white" d="M17 8h2l2 3v6a2.5 2.5 0 0 1-5 0v-3h2v3a.5.5 0 0 0 1 0v-5l-2-2zM2 20h17v2H2z"/>',
    lastebil: '<path fill="white" d="M2 5h12v12H2zM15 9h4l3 4v4h-7z"/><circle fill="white" cx="6" cy="18" r="2.5"/><circle fill="white" cx="18.5" cy="18" r="2.5"/>',
    radiomast: '<path fill="none" stroke="white" stroke-width="2" stroke-linecap="round" d="M8.5 4.5a5 5 0 0 0 0 7M15.5 4.5a5 5 0 0 1 0 7M5.2 2a9 9 0 0 0 0 12M18.8 2a9 9 0 0 1 0 12"/><circle fill="white" cx="12" cy="8" r="2"/><path fill="white" d="M11 10.5h2l3.5 11.5h-2.3l-.7-2.5h-3l-.7 2.5H7.5z"/>',
    lyspaere: '<path fill="white" d="M12 2a7 7 0 0 0-4.2 12.6c1 .8 1.4 1.8 1.5 3.4h5.4c.1-1.6.5-2.6 1.5-3.4A7 7 0 0 0 12 2zM9 19h6v2H9zM10 22h4v1h-4z"/>',
    termometer: '<path fill="white" fill-rule="evenodd" d="M12 2a4 4 0 0 0-4 4v9.1a6 6 0 1 0 8 0V6a4 4 0 0 0-4-4zm0 2a2 2 0 0 1 2 2v10.3a4 4 0 1 1-4 0V6a2 2 0 0 1 2-2z"/><path fill="white" d="M11 8h2v9h-2z"/><circle fill="white" cx="12" cy="18" r="2.5"/>',
    batteri: '<rect fill="white" x="1" y="6" width="18" height="12" rx="2"/><rect fill="white" x="20" y="10" width="2" height="4"/><path fill="currentColor" d="m11.5 7.5-5.5 6h4l-1 3.5 5.5-6h-4z"/>',
    eske: '<path fill="white" d="M12 2 2 7v10l10 5 10-5V7z"/><path fill="none" stroke="currentColor" stroke-width="1.5" stroke-linejoin="round" d="m2 7 10 5 10-5M12 12v10M7 4.5l10 5"/>'
};

// Hvilket symbol hver type får. Navnene er de samme som i enumene
// BehovType og RessursType (Models/Enums.cs). Behov og ressurser som passer
// sammen får samme symbol, f.eks. Nodstrom (behov) og Aggregat (ressurs).
const SYMBOL_FOR_TYPE = {
    Nodstrom: 'lyn',
    Aggregat: 'lyn',
    Drivstofftransport: 'drivstoff',
    Drivstoff: 'drivstoff',
    Transport: 'lastebil',
    Kommunikasjonsutstyr: 'radiomast',
    Nodbelysning: 'lyspaere',
    Oppvarming: 'termometer',
    Ups: 'batteri',
    Annet: 'eske'
};

/**
 * Lager SVG-koden for ett ikon.
 *  form      'behov' eller 'ressurs'
 *  typeKode  navnet fra enumen, f.eks. 'Nodstrom' eller 'Ups'
 *  storrelse bredde og høyde i piksler
 * Ukjente typer får eske-symbolet.
 */
function lagIkonSvg(form, typeKode, storrelse) {
    const symbol = KART_SYMBOLER[SYMBOL_FOR_TYPE[typeKode]] ?? KART_SYMBOLER.eske;

    // Nålen er rund øverst, så symbolet flyttes litt opp for å havne midt i den runde delen.
    const flyttNed = form === 'behov' ? 3.6 : 4.8;

    return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="' + storrelse + '" height="' + storrelse + '" aria-hidden="true">' +
        KART_FORMER[form] +
        // Symbolet krympes til 60 % (scale) og flyttes inn i midten av formen (translate).
        '<g transform="translate(4.8 ' + flyttNed + ') scale(0.6)">' + symbol + '</g>' +
        '</svg>';
}

/**
 * Lager et Leaflet-ikon (markør) til kartet.
 *  fargeKlasse 'bp-ikon-akutt', 'bp-ikon-planlagt' eller 'bp-ikon-ressurs' (farger i site.css)
 */
function lagKartikon(form, typeKode, fargeKlasse) {
    const storrelse = 36;
    return L.divIcon({
        className: '',   // fjerner Leaflets egen stil
        html: '<span class="bp-kartikon ' + fargeKlasse + '">' + lagIkonSvg(form, typeKode, storrelse) + '</span>',
        iconSize: [storrelse, storrelse],
        // Hvilket punkt i ikonet som peker på stedet: spissen nederst på nålen,
        // midten på firkanten.
        iconAnchor: form === 'behov' ? [storrelse / 2, storrelse - 2] : [storrelse / 2, storrelse / 2],
        // Hvor boblen åpnes, i forhold til ankerpunktet (litt over ikonet).
        popupAnchor: [0, form === 'behov' ? -storrelse + 4 : -storrelse / 2]
    });
}
