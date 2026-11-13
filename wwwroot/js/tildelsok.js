// =========================================================================
// tildelsok.js
// Søk i forslagene på Admin › Tildel ressurs (Views/Admin/Tildel.cshtml).
// Med «Vis alle typer» kan lista bli lang, og da er det nyttig å søke på adresse eller type.
// sokeord() og passerSok() kommer fra sokefelt.js, som må lastes før denne filen.
//
// En ressurs som er krysset av, vises alltid, selv om den ikke passer søket.
// Da ser admin alltid hva som blir tildelt når man trykker «Tildel valgte».
// =========================================================================

const tildelSok = document.getElementById('tildel-sok');

// Lista finnes bare når det er forslag. Uten forslag er det heller ikke noe søkefelt.
if (tildelSok) {
    const forslag = [...document.querySelectorAll('[data-forslag]')];
    const tomTekst = document.getElementById('tildel-tom');

    /** Viser forslagene som passer søket, pluss de som er krysset av. */
    function sokIForslag() {
        const ord = sokeord(tildelSok);
        let antallSynlige = 0;

        for (const rad of forslag) {
            const erValgt = rad.querySelector('input[type="checkbox"]').checked;
            const passer = erValgt || passerSok(rad.textContent, ord);
            rad.hidden = !passer;
            if (passer) {
                antallSynlige++;
            }
        }

        tomTekst.hidden = antallSynlige > 0;
    }

    tildelSok.addEventListener('input', sokIForslag);
    sokIForslag();   // i tilfelle nettleseren husket søket fra sist (tilbake-knappen)
}
