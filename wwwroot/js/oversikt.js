// =========================================================================
// oversikt.js
// Kjører på Oversikt (Views/Oversikt/Index.cshtml).
// Lå før direkte i HTML-en, men er flyttet hit så Content-Security-Policy kan
// forby skript skrevet inn i sidene (se Sikkerhet/SikkerhetsheadereMiddleware.cs).
// lagIkonSvg() kommer fra kartikoner.js.
// =========================================================================

// Setter inn kartikonet foran hvert behov og hver ressurs i listene.
// Typen står i data-ikon-type. data-ikon-form="ressurs" gir firkant, ellers blir det nål (behov).
document.querySelectorAll('[data-ikon-type]').forEach(function (element) {
    var form = element.dataset.ikonForm || 'behov';
    element.innerHTML = lagIkonSvg(form, element.dataset.ikonType, 32);
});

// Viser klokkeslettet siden ble hentet, i brukerens egen tid (f.eks. 14:32).
document.getElementById('oppdatert-tid').textContent =
    new Date().toLocaleTimeString('no-NO', { hour: '2-digit', minute: '2-digit' });
