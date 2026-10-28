// =========================================================================
// Den store menyen (#bp-meny i _Layout.cshtml)
//  - "Meny" og "Søk" i toppen åpner menyen ("Søk" setter også markøren i søkefeltet)
//  - "Lukk", Escape-tasten eller et klikk på en lenke lukker den
//  - Søkefeltet filtrerer lenkene som finnes i menyen fra før
// =========================================================================
(function () {
    const meny = document.getElementById("bp-meny");
    if (!meny) return;

    const apneKnapper = document.querySelectorAll("[data-bp-meny-apne]");
    const sokFelt = document.getElementById("bp-sok-felt");
    const sokSkjema = meny.querySelector("[data-bp-sok-skjema]");
    const treffListe = meny.querySelector("[data-bp-sok-treff]");
    let sistFokusert = null;   // knappen som åpnet menyen, får fokus tilbake når den lukkes

    function apneMeny(medSok) {
        sistFokusert = document.activeElement;
        meny.hidden = false;
        document.body.classList.add("bp-meny-apen");   // hindrer at siden bak scroller
        apneKnapper.forEach(k => k.setAttribute("aria-expanded", "true"));

        // Flytt fokus inn i menyen, slik at tastatur- og skjermleserbrukere havner riktig.
        const mal = medSok ? sokFelt : meny.querySelector("[data-bp-meny-lukk]");
        if (mal) mal.focus();
    }

    function lukkMeny() {
        meny.hidden = true;
        document.body.classList.remove("bp-meny-apen");
        apneKnapper.forEach(k => k.setAttribute("aria-expanded", "false"));
        if (sistFokusert) sistFokusert.focus();
    }

    apneKnapper.forEach(knapp => {
        knapp.addEventListener("click", () => apneMeny(knapp.hasAttribute("data-bp-meny-sok")));
    });

    meny.querySelectorAll("[data-bp-meny-lukk]").forEach(k => k.addEventListener("click", lukkMeny));

    document.addEventListener("keydown", e => {
        if (e.key === "Escape" && !meny.hidden) lukkMeny();
    });

    // Lenker til et avsnitt på samme side (f.eks. #slik-fungerer-det) skal også lukke menyen.
    meny.addEventListener("click", e => {
        if (e.target.closest("a")) lukkMeny();
    });

    // ---------------------------------------------------------------------
    // Søk: samler alle lenkene i menyen én gang, og viser de som matcher teksten.
    // ---------------------------------------------------------------------
    if (!sokFelt || !treffListe) return;

    const alleLenker = [];
    const settUrl = new Set();
    meny.querySelectorAll(".bp-meny-hoved a, .bp-meny-midt details a").forEach(a => {
        const tekst = a.textContent.trim();
        if (!tekst || settUrl.has(a.href + tekst)) return;
        settUrl.add(a.href + tekst);
        // Ta med gruppenavnet (f.eks. "Behov") så "behov" også finner "Meld inn nytt behov".
        const gruppe = a.closest("details")?.querySelector("summary")?.textContent.trim() ?? "";
        alleLenker.push({ tekst, href: a.href, sokeTekst: (tekst + " " + gruppe).toLowerCase() });
    });

    function visTreff() {
        const sok = sokFelt.value.trim().toLowerCase();
        treffListe.innerHTML = "";
        if (!sok) return;

        const treff = alleLenker.filter(l => l.sokeTekst.includes(sok));
        if (treff.length === 0) {
            const li = document.createElement("li");
            li.className = "bp-sok-ingen";
            li.textContent = "Ingen treff i menyen.";
            treffListe.appendChild(li);
            return;
        }

        treff.forEach(l => {
            const li = document.createElement("li");
            const a = document.createElement("a");
            a.href = l.href;
            a.textContent = l.tekst;
            li.appendChild(a);
            treffListe.appendChild(li);
        });
    }

    sokFelt.addEventListener("input", visTreff);

    // Enter går til første treff.
    sokSkjema.addEventListener("submit", e => {
        e.preventDefault();
        const forste = treffListe.querySelector("a");
        if (forste) window.location.href = forste.href;
    });
})();
