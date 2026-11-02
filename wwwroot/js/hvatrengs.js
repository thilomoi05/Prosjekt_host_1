// =========================================================================
// hvatrengs.js
// Kjører på Hva trengs (Views/HvaTrengs/Index.cshtml).
// Lå før direkte i HTML-en, men er flyttet hit så Content-Security-Policy kan
// forby skript skrevet inn i sidene (se Sikkerhet/SikkerhetsheadereMiddleware.cs).
// lagIkonSvg() kommer fra kartikoner.js.
// =========================================================================

// Setter inn kartikonet for hver type utstyr. Typen står i data-ikon-type.
document.querySelectorAll('[data-ikon-type]').forEach(function (element) {
    element.innerHTML = lagIkonSvg('behov', element.dataset.ikonType, 40);
});
