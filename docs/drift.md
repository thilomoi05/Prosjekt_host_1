# Drift

Denne siden forklarer hvordan Beredskapsportalen startes, enten lokalt med .NET eller i Docker.

## Forutsetninger

- **Docker Desktop**, for å kjøre appen i Docker (anbefalt)
- **.NET 10 SDK**, bare nødvendig for å kjøre appen uten Docker

## Kjøre appen i Docker

Fra rotmappa i prosjektet (der `Dockerfile` ligger):

```bash
docker build -t beredskapsportal .
docker run --rm -p 8080:8080 --name beredskapsportal beredskapsportal
```

Åpne **http://localhost:8080** i nettleseren. Stopp appen med `Ctrl + C`.

| Del av kommandoen | Hva den gjør |
|---|---|
| `docker build -t beredskapsportal .` | Bygger et image fra `Dockerfile` og kaller det `beredskapsportal` |
| `-p 8080:8080` | Kobler port 8080 på maskinen til port 8080 i containeren |
| `--rm` | Sletter containeren automatisk når den stoppes |
| `--name beredskapsportal` | Gir containeren et navn, så den er lett å finne i Docker Desktop |

### Hvordan Docker-oppsettet fungerer

`Dockerfile` bygger appen i to steg:

1. **Bygg:** `mcr.microsoft.com/dotnet/sdk:10.0` henter pakker (`dotnet restore`) og kompilerer appen (`dotnet publish`).
2. **Kjøring:** den ferdige appen kopieres over i det mindre imaget `mcr.microsoft.com/dotnet/aspnet:10.0`, som bare inneholder det som trengs for å kjøre den.

Appen kjører som en vanlig bruker, ikke root. Mappa `Opplastinger/bilder`, der opplastede bilder av ressurser lagres, opprettes og får skrivetilgang i Dockerfile.

`.dockerignore` holder `bin/`, `obj/`, `.git/` og lokale opplastinger utenfor bygget.

## Kjøre appen uten Docker

```bash
dotnet run
```

Åpne **http://localhost:5220** i nettleseren.

## Testbruker

En testbruker opprettes automatisk når appen starter:

| Brukernavn | Passord |
|---|---|
| `testbruker` | `TestPassord123!` |

## Kjente begrensninger

- **Data lagres bare i minnet.** Brukere, behov og ressurser forsvinner når appen eller containeren stoppes.
- **Opplastede bilder forsvinner** når containeren slettes.
- **Innloggede brukere logges ut** når containeren startes på nytt. Nøklene som signerer innloggings-cookien lagres inne i containeren. Dette gir advarslene `DataProtection-Keys ... may not be persisted` og `No XML encryptor configured` ved oppstart.
- **Bare http i Docker.** Advarselen `Failed to determine the https port for redirect` er forventet, og appen fungerer som normalt over http.

## Feilsøking

| Problem | Løsning |
|---|---|
| `Cannot connect to the Docker daemon` | Start Docker Desktop, og vent til det står «Engine running» |
| `port is already allocated` | Bruk en annen port, f.eks. `-p 8081:8080`, og åpne http://localhost:8081 |
| `open Dockerfile: no such file or directory` | Kjør kommandoen fra rotmappa i prosjektet (sjekk med `pwd`) |