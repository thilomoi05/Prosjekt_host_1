// =========================================================================
// detaljkart.js
// Kjører på detaljsidene (Views/Behov/Detaljer.cshtml og Views/Ressurs/Detaljer.cshtml).
//
// Hva filen gjør:
//  1. Setter inn ikonene (samme som på kartet) der det står data-ikon-type.
//  2. Tegner et lite kart med punktet. Koordinatene og ikonet står i data-* på #detaljkart.
//  3. Får "Tilbake"-lenken til å gå til forrige side i stedet for en fast side.
//
// lagKart() kommer fra kartverket.js, lagIkonSvg() og lagKartikon() fra kartikoner.js.
// =========================================================================

// 1. Ikonene. data-ikon-form="ressurs" gir firkant, ellers blir det nål (behov).
document.querySelectorAll('[data-ikon-type]').forEach(element => {
    const form = element.dataset.ikonForm || 'behov';
    const storrelse = Number(element.dataset.ikonStorrelse || 32);
    element.innerHTML = lagIkonSvg(form, element.dataset.ikonType, storrelse);
});

// 2. Det lille kartet
const kartElement = document.getElementById('detaljkart');
if (kartElement) {
    const posisjon = [Number(kartElement.dataset.breddegrad), Number(kartElement.dataset.lengdegrad)];

    // scrollWheelZoom: false gjør at siden ikke zoomer kartet når man bare vil scrolle forbi det.
    const kart = lagKart('detaljkart');
    kart.scrollWheelZoom.disable();
    kart.setView(posisjon, 14);

    const ikon = lagKartikon(kartElement.dataset.form, kartElement.dataset.type, kartElement.dataset.farge);
    L.marker(posisjon, { icon: ikon }).addTo(kart);
}

// 3. "Tilbake": har brukeren kommet fra en annen side i portalen, går vi tilbake dit
//    (da beholdes også filteret i lista). Ellers følges den vanlige lenken (href).
const tilbakeLenke = document.querySelector('[data-tilbake]');
if (tilbakeLenke && document.referrer.startsWith(window.location.origin)) {
    tilbakeLenke.addEventListener('click', function (hendelse) {
        hendelse.preventDefault();
        history.back();
    });
}
