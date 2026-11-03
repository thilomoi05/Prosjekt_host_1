// =========================================================================
// ressursrediger.js
// Kjører på siden "Endre ressurs" (Views/Ressurs/Rediger.cshtml).
//
// Hva filen gjør:
//  1. Setter inn ikonene på typeknappene (samme ikoner som på kartet).
//  2. Viser små forhåndsvisninger av nye bilder brukeren har valgt.
// Kartet og adressesøket styres av posisjonsvelger.js, som også viser
// markøren der ressursen er lagret nå (Breddegrad og Lengdegrad er fylt ut).
// Skriptet ligger i egen fil fordi Content-Security-Policy ikke tillater skript i HTML-en.
// =========================================================================

// lagIkonSvg kommer fra kartikoner.js.
document.querySelectorAll('.bp-type-knapp [data-ikon-type]').forEach(element => {
    element.innerHTML = lagIkonSvg('ressurs', element.dataset.ikonType, 36);
});

const nyeBilder = document.getElementById('Bilder');
const forhandsvisning = document.getElementById('bilde-forhandsvisning');

nyeBilder.addEventListener('change', function () {
    forhandsvisning.innerHTML = '';
    for (const fil of nyeBilder.files) {
        // En midlertidig adresse til bildet på brukerens egen maskin, så det kan vises før lagring.
        const bilde = document.createElement('img');
        bilde.src = URL.createObjectURL(fil);
        bilde.alt = fil.name;
        forhandsvisning.appendChild(bilde);
    }
});
