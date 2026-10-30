# Drift

Denne siden forklarer hvordan Beredskapsportalen startes, enten med Docker Compose eller lokalt med .NET.

## Forutsetninger

- **Docker Desktop**, for å kjøre appen og databasen i Docker (anbefalt)
- **.NET 10 SDK**, bare nødvendig for å kjøre appen uten Docker

## Første gang: lag .env

Passordene til databasen ligger i en lokal fil `.env` som git ignorerer. Kopier malen og sett egne passord:

```bash
cp .env.example .env
```

```
DB_ROOT_PASSWORD=ditt-root-passord
DB_PASSWORD=ditt-app-passord
```

Bruk minst 16 tegn og ulike passord. Bruk bare bokstaver, tall, `-` og `_`. Del aldri `.env` på GitHub eller i chat.

## Kjøre appen i Docker

Fra rotmappa i prosjektet (der `docker-compose.yml` ligger):

```bash
docker compose up --build
```

Åpne **http://localhost:8080** i nettleseren. Stopp med `Ctrl + C`.

| Kommando | Hva den gjør |
|---|---|
| `docker compose up --build` | Bygger appen og starter app og database |
| `docker compose up -d --build` | Samme, men i bakgrunnen |
| `docker compose down` | Stopper og fjerner containerne. Dataene beholdes |
| `docker compose down -v` | Sletter også databasen og opplastede bilder |

### Hvordan Docker-oppsettet fungerer

`docker-compose.yml` starter to containere:

1. **db:** MariaDB 11.4. Databasen `beredskapsportal` og brukeren `beredskap` opprettes ved første oppstart. Dataene lagres i volumet `dbdata`.
2. **web:** appen, bygget fra `Dockerfile`. Den starter først når databasen svarer (healthcheck).

Appen finner databasen gjennom miljøvariabelen `ConnectionStrings__BeredskapsDb`. Passordet hentes fra `.env`.

Ved oppstart kjører appen migrations fra mappa `Migrations` (`DatabaseOppsett.cs`). Da lages eller oppdateres tabellene `Brukere`, `Behov`, `Ressurser` og `Tildelinger`. Er tabellene tomme, fylles de med eksempeldata.

Begge containerne er bare tilgjengelige fra egen maskin (`127.0.0.1`).

`Dockerfile` bygger appen i to steg:

1. **Bygg:** `mcr.microsoft.com/dotnet/sdk:10.0` henter pakker (`dotnet restore`) og kompilerer appen (`dotnet publish`).
2. **Kjøring:** den ferdige appen kopieres over i det mindre imaget `mcr.microsoft.com/dotnet/aspnet:10.0`.

Appen kjører som en vanlig bruker, ikke root. `.dockerignore` holder `bin/`, `obj/`, `.git/`, `.env` og lokale opplastinger utenfor bygget.

## Kjøre appen uten Docker

Start bare databasen i Docker:

```bash
docker compose up -d db
```

Lagre tilkoblingsstrengen med User Secrets. Bruk `DB_PASSWORD` fra `.env`:

```bash
dotnet user-secrets set "ConnectionStrings:BeredskapsDb" "Server=localhost;Port=3306;Database=beredskapsportal;User=beredskap;Password=DITT-APP-PASSORD"
```

Start appen:

```bash
dotnet run
```

Åpne **http://localhost:5220** i nettleseren.

Uten tilkoblingsstreng bruker appen minnebasert lagring, som før. Det brukes også av testene.

## Endre databasen

Tabellene styres av C#-modellene og `Data/BeredskapsDbContext.cs`. Endre aldri tabeller direkte med SQL.

1. Endre modellen eller `BeredskapsDbContext`
2. Lag en migration: `dotnet ef migrations add BeskrivendeNavn`
3. Commit filene i `Migrations`
4. Neste gang appen starter, oppdateres databasen. Det gjelder hos alle i gruppa

Krever verktøyet `dotnet-ef`: `dotnet tool install --global dotnet-ef --version 10.0.13`

## Se inni databasen

```bash
docker exec -it beredskap-db mariadb -u beredskap -p beredskapsportal
```

```sql
SHOW TABLES;
SELECT Id, Type, Status FROM Behov;
```

## Testbrukere

To testbrukere opprettes automatisk ved første oppstart, én for hver rolle:

| Brukernavn | Passord | Rolle | Hva du ser |
|---|---|---|---|
| `test` | `test` | Offentlig aktør (f.eks. Heimevernet) | Oversikt, kart, behov, ressurser og hva som trengs |
| `privat` | `privat` | Privat aktør (bedrift eller privatperson) | Egne ressurser, tilby ressurs og hva som trengs |

Testbrukerne er bare for testing og skal fjernes før portalen tas i bruk.

## Kjente begrensninger

- **Passord lagres i klartekst** i tabellen `Brukere`. Skal byttes til ASP.NET Core Identity med hashing.
- **Innloggede brukere logges ut** når containeren startes på nytt. Nøklene som signerer innloggings-cookien lagres inne i containeren. Dette gir advarslene `DataProtection-Keys ... may not be persisted` og `No XML encryptor configured` ved oppstart.
- **Bare http i Docker.** Advarselen `Failed to determine the https port for redirect` er forventet.
- **Hver person har sin egen lokale database.** Data du registrerer vises bare hos deg.

## Feilsøking

| Problem | Løsning |
|---|---|
| `Cannot connect to the Docker daemon` | Start Docker Desktop, og vent til det står «Engine running» |
| `port is already allocated` | Noe annet bruker port 8080 eller 3306. Endre porten i `docker-compose.yml` |
| `no configuration file provided` | Kjør kommandoen fra rotmappa i prosjektet |
| `Access denied for user 'beredskap'` | Passordet i `.env` er endret etter at databasen ble laget. Kjør `docker compose down -v` og start på nytt |
| `En programkontrollpolicy har blokkert denne filen` | Windows Smart App Control. Løst med `UseAppHost=false` i `Beredskapsportal.csproj` |