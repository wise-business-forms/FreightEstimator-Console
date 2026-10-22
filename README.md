# FreightEstimator-Console

## Overview
FreightEstimator-Console is a legacy Windows console application used to process freight rating requests from a SQL queue, call external carrier/rating services, and write results back to the database.

Primary runtime project:
- FreightEstApp35

## Runtime and Framework
- Language: C#
- Application type: Console app
- Framework: .NET 10 (`net10.0`), upgraded from .NET Framework 4.8. See `docs/adr/0003-net10-self-contained-in-place-cutover.md`.
- Build system: SDK-style projects, built with the .NET 10 SDK (`dotnet build`)
- NuGet style: `PackageReference` (no `packages.config`)
- Deployment: self-contained, single-file `dotnet publish`; the production host needs no .NET runtime installed. See [Building and publishing](#building-and-publishing).

## What the App Does
At startup, the app:
- Prints launch and version information.
- Loads runtime configuration.
- Enters a continuous processing loop.

Core processing behavior:
- Polls the request queue through DBUtil and Request classes.
- Validates request/address data.
- Requests UPS and/or LTL rates through UpsComm and related providers.
- Saves rate results back to SQL.
- Sleeps briefly when no work is available, then polls again.

Important runtime characteristic:
- The app runs continuously (service-like loop) until stopped.

## Key Dependencies
- SQL Server database (UpsRate and related linked objects)
- UPS REST APIs (OAuth, address validation, rating)
- LTL provider integrations (for example Transportation Insight, Transplace, M33)
- NuGet packages: `Microsoft.Data.SqlClient`, `Newtonsoft.Json`, `System.Configuration.ConfigurationManager` (see `FreightEstApp35/FreightEstApp35.csproj`)

The legacy UPS SOAP web references and the external `SoapTrace.dll` were removed in the .NET 10 upgrade; only the REST client is used.

## Configuration
Main configuration files:
- FreightEstApp35/App.config
- FreightEstApp35/Config.cs

**Where the deployed app reads its settings.** A published single-file build reads `FreightEstApp35.dll.config` (a copy of `App.config`) from the folder next to the exe. It does **not** read `FreightEstApp35.exe.config`: a copy with that name is silently ignored and the app starts with null settings. Edit the `.dll.config` file on the server. (During development `dotnet run` and the harness use the `.dll.config` in the build output.)

**SQL connections** use `Microsoft.Data.SqlClient` with `Encrypt=False;TrustServerCertificate=True` set explicitly in `Config.cs`, which preserves the connection behavior of the old `System.Data.SqlClient`. That is not a security decision; tightening it is a separate change.

Configuration includes:
- SQL server and connection settings
- UPS and LTL endpoint URLs
- Access tokens, credentials, and API secrets
- Environment toggles and plant shipping metadata

Security note:
- This repository currently contains plaintext credentials/secrets in configuration/code. Treat this repo as sensitive and rotate/move secrets to secure storage when possible.

## Carrier API Logging
Every physical call this app makes to a carrier's API (UPS, TransPlace, TransportationInsight) is logged, insert-only, across two tables in the `UpsRate` database:

- **`dbo.CarrierApiCall`** — one row per HTTP call: provider, shipping mode, plant, URL, status code, success flag, duration, raw request/response bodies, and an `IsTest` flag.
- **`dbo.CarrierRateQuote`** — one row per priced service line a call returned (a call can return several), FK'd to its `CarrierApiCallId`: carrier name, service level, and the cost breakdown (`BaseCost`, `MarkupPercent`, `PerPackageCharge`, `PerShipmentCharge`, `TotalCharge`).

All calls belonging to one shipment request share a `CorrelationId`, which is the join key across sibling calls (e.g. UPS + TransPlace + TransportationInsight for the same request). See `docs/adr/0001-carrier-api-call-quote-schema.md` for why the schema is normalized and insert-only, and `CONTEXT.md` for the full domain vocabulary (ApiProvider, CarrierName, ShippingMode, Carrier API Call, Carrier Rate Quote, Test Call).

Logging failures are swallowed (caught and printed to the console) so a DB hiccup never breaks the primary rate-fetching flow — see `FreightEstApp35/CarrierApiLogger.cs`.

## Testing with the Carrier Test Harness
`CarrierTestHarness` is a small standalone console app, separate from the always-running `FreightEstApp35` service, that lets you trigger one shipment request on demand (e.g. from Postman) and read back the `CarrierApiCall`/`CarrierRateQuote` rows it produced — proof that logging actually persisted, since the logger swallows write failures rather than throwing.

Every request made through the harness is a **Test Call** (`IsTest = true`).

Important caveat (see `docs/adr/0002-ups-test-calls-hit-production.md`): UPS has no usable sandbox for the REST Rating API this app calls, so UPS Test Calls still hit **production UPS** — `IsTest` is the only thing distinguishing them from real traffic. TransPlace and TransportationInsight Test Calls do route to their real demo/UAT endpoints instead of production.

### Running the harness
The harness is an SDK-style `net10.0` project, so `dotnet` runs it directly:

```
dotnet run --project CarrierTestHarness
```

It listens on `http://localhost:5085/` (localhost only) and logs the environment/connection string it resolved on startup, same as the main app. Its connection settings live in `CarrierTestHarness/App.config` — currently pointed at the same `UpsRate` database as production, so Test Call rows land in the real `CarrierApiCall`/`CarrierRateQuote` tables alongside live traffic (filter them out with `IsTest = 1` when querying, see below). To keep test traffic out of production data, point the harness at the SANDBOX server instead, as described in `docs/parity/README.md` (edit the build-output `.dll.config`, not the tracked file).

### Sending a test request
POST a JSON body to `http://localhost:5085/test-request`. All fields are optional — omitted fields fall back to sensible defaults (plant `ALP`, freight class `55`, 1 package at 10 lbs, both UPS and LTL requested):

```json
{
  "correlationId": "manual-test-001",
  "fromPlant": "ALP",
  "toAddress": { "street": "123 Main St", "city": "Atlanta", "state": "GA", "zip": "30301", "country": "US" },
  "freightClass": "55",
  "numPackages": 1,
  "pkgWeight": 10,
  "requestUPS": true,
  "requestLTL": true
}
```

If `correlationId` is omitted, the harness generates one (`harness-test-<guid>`) and echoes it back — save it, since it's what you'll use to look up rows afterward.

The response includes:
- `upsRates` / `ltlRates` — the rate quotes returned in-process (what the caller sees).
- `loggedCalls` — the `CarrierApiCall` rows (with nested `Quotes`) that were just read back from the database for this `CorrelationId`, proving the write succeeded.
- `processingError` — populated if rate-fetching threw; logging still runs for whatever calls completed before the error.

A `loggedCalls` array that's shorter than expected (or empty) means logging failed silently — check the harness console output for a "Warning: failed to write..." line, then confirm DB connectivity/table existence.

## Verifying results with SQL
Run these against the `UpsRate` database (same server/credentials as `CarrierTestHarness/App.config` and `FreightEstApp35/App.config`) to double-check what the harness (or the live app) wrote.

Look up everything for one request by `CorrelationId`:

```sql
SELECT * FROM dbo.CarrierApiCall
WHERE CorrelationId = 'manual-test-001'
ORDER BY Id;

SELECT q.*
FROM dbo.CarrierRateQuote q
JOIN dbo.CarrierApiCall c ON c.Id = q.CarrierApiCallId
WHERE c.CorrelationId = 'manual-test-001'
ORDER BY q.CarrierApiCallId, q.Id;
```

Recent Test Calls only (harness traffic, excluding real shipment requests):

```sql
SELECT TOP 50 Id, CorrelationId, ApiProvider, ShippingMode, Operation, PlantCode,
       IsSuccess, HttpStatusCode, DurationMs, RequestedAtUtc
FROM dbo.CarrierApiCall
WHERE IsTest = 1
ORDER BY Id DESC;
```

Calls and their quotes together, one row per quote line:

```sql
SELECT c.CorrelationId, c.ApiProvider, c.ShippingMode, c.PlantCode, c.IsSuccess, c.IsTest,
       q.CarrierName, q.ServiceLevel, q.BaseCost, q.MarkupPercent, q.TotalCharge, q.Currency
FROM dbo.CarrierApiCall c
LEFT JOIN dbo.CarrierRateQuote q ON q.CarrierApiCallId = c.Id
WHERE c.CorrelationId = 'manual-test-001'
ORDER BY c.Id, q.Id;
```

Failed calls, to check what a bad harness run actually recorded:

```sql
SELECT Id, CorrelationId, ApiProvider, Operation, HttpStatusCode, ErrorMessage, RequestedAtUtc
FROM dbo.CarrierApiCall
WHERE IsSuccess = 0
ORDER BY Id DESC;
```

Note: real fee-trend queries against this data should exclude Test Calls (`WHERE IsTest = 0`), since UPS Test Calls hit production and would otherwise skew cost comparisons.

## Building and publishing
Needs the .NET 10 SDK. From the repo root:

```
dotnet build FreightEstApp35.sln
dotnet test FreightEstApp35.sln
```

To produce the deployable build (replaces the old ClickOnce publish):

```
dotnet publish FreightEstApp35 -p:PublishProfile=SelfContainedSingleFile
```

Output is `FreightEstApp35\bin\publish\`: `FreightEstApp35.exe` (self-contained and single-file, with the SQL native library bundled in) and `FreightEstApp35.dll.config`. See the comments in `FreightEstApp35/Properties/PublishProfiles/SelfContainedSingleFile.pubxml` for why those choices matter. The profile is win-x64 only.

**Do not just run the app to "see if it works".** `dotnet run` and the published exe poll the queue named by the configuration and write results back. With the checked-in settings that is the production queue.

- Deploying to production, and rolling back: `docs/runbooks/net10-cutover.md`.
- Proving a build behaves like the old one (harness comparison and a safe SANDBOX test of the published exe): `docs/parity/README.md`.

## Running and Debugging in VS Code
The `.vscode` tasks and launch profile build with `dotnet build` and debug with the .NET debugger (`coreclr`). Before starting a debug session, point the `.dll.config` in the build output (`FreightEstApp35/bin/Debug/net10.0/`) at a non-production queue, because the app starts polling immediately.

## Known Operational Notes
- The processing loop is infinite by design.
- No queued requests means idle polling/sleep behavior.
- Runtime success depends on network access to SQL and external carrier endpoints.
- The queue has no claim step: a request is picked with a plain `SELECT TOP 1 ... WHERE DateProcessed IS NULL OR DateRated IS NULL`. **Never run two instances against the same queue**; both will process the same request.
- At startup the app prints a status banner (plant, database servers, connectivity checks, accounts, runtime) to stdout. The SQL password is never printed. The SQL login is, so a startup log still names the account in use.

## Troubleshooting Quick Checks
- A published exe that starts but behaves as if every setting is missing is reading no config: confirm the file is named `FreightEstApp35.dll.config` (not `.exe.config`) and sits next to the exe.
- If SQL access fails immediately with a `TypeInitializationException` for `TdsParser`, the SQL native library is missing from the build; publish with the provided profile, which bundles it.
- If the app starts then exits immediately, verify database connectivity and required configuration values.
- If a build fails on restore, check that the .NET 10 SDK is installed (`dotnet --list-sdks`).
