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
DB_PORT=3306
TESTBRUKER_PASSORD=passordet-til-testbrukerne
```

`DB_PORT` er porten databasen får på din maskin. Er 3306 opptatt, bruk 3307 (se [Feilsøking](#feilsøking)).

`TESTBRUKER_PASSORD` er passordet til testbrukerne (se [Testbrukere](#testbrukere)). Er det tomt, lages ingen testbrukere.

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

Ved oppstart kjører appen migrations fra mappa `Migrations` (`DatabaseOppsett.cs`). Da lages eller oppdateres tabellene `Behov`, `Ressurser` og `Tildelinger`, og tabellene til ASP.NET Core Identity (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles` med flere). Rollene og testbrukerne lages av `Data/IdentitetOppsett.cs`. Er tabellene tomme, fylles de med eksempeldata.

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
dotnet user-secrets set "Testbrukere:Passord" "PASSORDET-TIL-TESTBRUKERNE"
```

Start appen:

```bash
dotnet run
```

Åpne **http://localhost:5220** i nettleseren.

Uten tilkoblingsstreng stopper appen med en feilmelding, fordi innloggingen (ASP.NET Core Identity) lagrer brukerne i databasen. Testene trenger ikke database.

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

Tre testbrukere opprettes automatisk ved oppstart (`Data/IdentitetOppsett.cs`), hvis `TESTBRUKER_PASSORD` er satt i `.env` (eller `Testbrukere:Passord` i User Secrets). Alle tre får det samme passordet. I gruppa bruker vi:

```
TESTBRUKER_PASSORD=Beredt2026
```

| Brukernavn | Passord | Rolle | Hva du ser |
|---|---|---|---|
| `test` | `Beredt2026` | Offentlig aktør (f.eks. Heimevernet) | Oversikt, kart, behov, ressurser og hva som trengs |
| `privat` | `Beredt2026` | Privat aktør (bedrift eller privatperson) | Egne ressurser, tilby ressurs og hva som trengs |
| `admin` | `Beredt2026` | Administrator (har også rollen offentlig aktør) | Alt en offentlig aktør ser, pluss adminsiden: godkjenne ressurser og offentlige aktører, og tildele ressurser til behov |

Passordet må følge passordreglene: minst 8 tegn, med stor og liten bokstav og tall. Ellers stopper appen med en feilmelding ved oppstart.

Testbrukerne lages bare hvis de ikke finnes fra før. Byttet du passord etter at de ble laget, slett databasen med `docker compose down -v` og start på nytt.

Testbrukerne er bare for testing og skal fjernes før portalen tas i bruk.

### Nye brukere og godkjenning

- **Privat aktør:** registrerer seg på `/Konto/Registrer` og logges inn med en gang.
- **Offentlig aktør:** registrerer seg på samme side, men kan ikke logge inn før en administrator har godkjent kontoen på adminsiden (`/Admin`). Til da står `VenterPaGodkjenning` til `true` i `AspNetUsers`.
- **Admin:** kan ikke velges ved registrering. Lages bare av `IdentitetOppsett`.

## Kjente begrensninger

- **Nye ressurser må godkjennes.** En ressurs som sendes inn, vises ikke på kartet før en administrator har godkjent den på `/Admin`. Logg inn som `admin` for å godkjenne.
- **Innloggede brukere logges ut** når containeren startes på nytt. Nøklene som signerer innloggings-cookien lagres inne i containeren. Dette gir advarslene `DataProtection-Keys ... may not be persisted` og `No XML encryptor configured` ved oppstart.
- **Bare http i Docker.** Advarselen `Failed to determine the https port for redirect` er forventet.
- **Hver person har sin egen lokale database.** Data du registrerer vises bare hos deg.

## Feilsøking

| Problem | Løsning |
|---|---|
| `Cannot connect to the Docker daemon` | Start Docker Desktop, og vent til det står «Engine running» |
| `port is already allocated` eller `ports are not available ... 3306 ... forbidden by its access permissions` | Port 3306 er opptatt. Enten kjører en lokal MySQL/MariaDB (stopp tjenesten i `services.msc`), eller Windows/Hyper-V har reservert porten (sjekk med `netsh interface ipv4 show excludedportrange protocol=tcp`). Enkleste løsning: sett `DB_PORT=3307` i `.env`. Bruker du `dotnet run`, må `Port=` i tilkoblingsstrengen i User Secrets da også være 3307 |
| Samme feil for port 8080 | Noe annet bruker 8080. Lukk programmet, eller endre `8080:8080` til f.eks. `8081:8080` i `docker-compose.yml` lokalt |
| `no configuration file provided` | Kjør kommandoen fra rotmappa i prosjektet |
| `Access denied for user 'beredskap'` | Passordet i `.env` er endret etter at databasen ble laget. Kjør `docker compose down -v` og start på nytt |
| `En programkontrollpolicy har blokkert denne filen` | Windows Smart App Control. Løst med `UseAppHost=false` i `Beredskapsportal.csproj` |