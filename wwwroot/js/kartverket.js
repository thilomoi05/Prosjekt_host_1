// =========================================================================
// kartverket.js
// Felles oppsett for alle kartene i appen (kartsiden og skjemaene).
// Ligger i egen fil, så vi slipper å skrive det samme flere steder.
//
// Leaflet (L) er kartbiblioteket vi bruker. Det lastes inn før denne filen,
// fra wwwroot/lib/leaflet/leaflet.js.
// =========================================================================

// Startpunkt for kartet: Kristiansand sentrum (breddegrad, lengdegrad).
const KART_START = [58.1467, 7.9956];
const KART_START_ZOOM = 12;

/**
 * Lager et Leaflet-kart i HTML-elementet med den gitte id-en,
 * med bakgrunnskart fra Kartverket.
 * Returnerer kartet, slik at den som kaller funksjonen kan legge til punkter.
 */
function lagKart(elementId) {
    const kart = L.map(elementId).setView(KART_START, KART_START_ZOOM);

    // Bakgrunnskartet hentes som små bilder ("fliser") rett fra Kartverket.
    // {z} = zoomnivå, {x} og {y} = hvilken flis. Leaflet fyller inn tallene selv.
    // Serveren vår sender aldri disse bildene. Nettleseren henter dem direkte.
    L.tileLayer('https://cache.kartverket.no/v1/wmts/1.0.0/topo/default/webmercator/{z}/{y}/{x}.png', {
        maxZoom: 18,
        attribution: '&copy; <a href="https://www.kartverket.no/">Kartverket</a>'
    }).addTo(kart);

    return kart;
}
