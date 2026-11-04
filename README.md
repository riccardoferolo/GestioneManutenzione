# Gestione Manutenzione

API REST per pianificare e seguire la manutenzione di macchine e impianti: anagrafica macchine e tecnici,
ordini di lavoro con un ciclo di vita controllato e piani di manutenzione preventiva che generano gli ordini in automatico.

![CI](https://github.com/riccardoferolo/GestioneManutenzione/actions/workflows/ci.yml/badge.svg)

## Funzionalità

- **Macchine:** codice univoco, reparto, numero di ordini aperti
- **Tecnici:** anagrafica e specializzazione
- **Ordini di lavoro:** priorità, scadenza, assegnazione del tecnico, filtri per stato, macchina e ordini scaduti
- **Ciclo di vita:** `Open → InProgress → Completed`, con annullamento possibile finché l'ordine non è chiuso.
  Un ordine non può partire senza tecnico e non può essere riaperto dopo la chiusura.
- **Manutenzione preventiva:** un piano con intervallo in giorni genera gli ordini fino all'orizzonte richiesto.
  La generazione è idempotente: rilanciarla non crea doppioni.

## Tecnologie

.NET 8 · ASP.NET Core Minimal API · Entity Framework Core · SQL Server · xUnit · Docker · GitHub Actions

## Architettura

```
src/Manutenzione.Api
├── Domain       entità e regole di business (transizioni di stato, scadenze dei piani)
├── Data         DbContext, mappatura e migrazioni EF Core
├── Contracts    DTO di richiesta e risposta con validazione
└── Endpoints    gruppi di endpoint per risorsa
tests/Manutenzione.Api.Tests
└── test di integrazione sull'API reale con SQLite in memoria e orologio fisso
```

Le regole di business stanno nelle entità del dominio, non negli endpoint: così si testano senza HTTP né database.

## Avvio rapido con Docker

```bash
cp .env.example .env        # imposta una password robusta per SQL Server
docker compose up --build
```

L'API è su `http://localhost:8080`, la documentazione Swagger su `http://localhost:8080/swagger`.
In ambiente Development le migrazioni vengono applicate all'avvio.

## Sviluppo locale

```bash
dotnet test
dotnet run --project src/Manutenzione.Api
```

Imposta la stringa di connessione con `ConnectionStrings__Maintenance` oppure con `dotnet user-secrets`.

## Esempi

```http
POST /api/assets
{ "code": "CNC-01", "name": "Fresatrice CNC", "location": "Reparto A" }

POST /api/plans
{ "assetId": 1, "title": "Lubrificazione mandrino", "intervalDays": 30, "priority": "High", "firstDueDate": "2026-11-10" }

POST /api/plans/generate?daysAhead=60

GET /api/work-orders?overdue=true
```

## Prossimi passi

- Interfaccia web in Quasar
- Autenticazione e ruoli (responsabile, tecnico)
- Storico interventi ed esportazione dei report
