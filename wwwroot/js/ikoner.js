// =========================================================================
// ikoner.js
// Setter inn kartikonene (samme som på kartet) der det står data-ikon-type.
// Brukes av sider som bare trenger ikonene, f.eks. Admin og Tildel.
//  - data-ikon-type:      typen, f.eks. "Nodstrom" eller "Aggregat"
//  - data-ikon-form:      "ressurs" gir firkant, ellers nål (behov)
//  - data-ikon-storrelse: størrelse i piksler (standard 28)
// lagIkonSvg() kommer fra kartikoner.js, som må lastes før denne filen.
// =========================================================================

document.querySelectorAll('[data-ikon-type]').forEach(function (element) {
    var form = element.dataset.ikonForm || 'behov';
    var storrelse = Number(element.dataset.ikonStorrelse || 28);
    element.innerHTML = lagIkonSvg(form, element.dataset.ikonType, storrelse);
});
