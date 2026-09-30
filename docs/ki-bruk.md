# Dokumentasjon av KI-bruk og prompter

I dette prosjektet har vi brukt kunstig intelligens (KI) som en aktiv samarbeidspartner og støtteverktøy gjennom hele prosessen for å øke produktiviteten, forstå teknologien bedre og sikre god kodekvalitet.

## 1. Verktøy vi har brukt
* **Claude (Anthropic):** Hovedverktøyet for koding, strukturering av arkitektur, feilsøking og skriving av dokumentasjon.
* **Trello:** Til planlegging av oppgaver og oversikt over arbeidsflyt.
* **Figma:** Brukt som utgangspunkt for design (som deretter ble oversatt til kode ved hjelp av KI).

## 2. Bruksområder
KI har vært involvert i følgende faser av prosjektet:
* **Idéutvikling og arkitektur:** Brainstorming rundt løsninger og struktur i ASP.NET Core MVC.
* **Koding og implementasjon:** Hjelp med å skrive oppsett, repositories og integrasjon med kart (Leaflet / Geonorge).
* **Feilsøking:** Analysere feilmeldinger og finne løsninger i koden eller Dockerfile.
* **Dokumentasjon:** Strukturere og skrive markdown-filer.
* **Planlegging:** Organisere oppgaver og arbeidsflyt (Trello).

## 3. Eksempler på konkrete prompter
Her er eksempler på prompter vi har brukt underveis:
1. «Bygg forsiden etter Figma-skissen, med innlogging og skjema for behov og ressurser»
   
2. «Ta meg gjennom Docker-oppsettet steg for steg og forklar hva som skjer underveis»
   
3. «Se gjennom repoet og sett opp en Trello-tavle med oppgavene som er gjort og det som gjenstår»

4.Her får du neste del av det jeg har denne sprinten: Vil at du skal gå i git repoet https://github.com/thilomoi05/Prosjekt_host_1 og gi inspirasjon med figurer og diagrammer hvordan arkitekturen er og hva vi skal gjøre videre / endre. Ønsker at du viser hvordan jeg kan sette opp diagrammer i markdown filer.

## 4. Retningslinjer og regler (AGENT.md)
Vi har fulgt reglene i `AGENT.md` for alt arbeid med KI:
* **Ingen direkte koding i main:** Alt arbeid skjer på egne branches (`feature/...`).
* **Krav om Pull Request:** Alle endringer går via PR.
* **Aldri selv-merge:** Ingen godkjenner eller merger sin egen PR – en annen på gruppa må sjekke og godkjenne.
* **Commit-merking:** Commits som er laget med KI er merket i loggen med:
  `Co-Authored-By: Claude`
