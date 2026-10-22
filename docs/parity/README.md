# Parity check: net48 baseline vs net10 build

Proves the upgraded app produces the same Carrier Rate Quotes as the old one, using the Carrier Test Harness as the single seam (see `docs/adr/0003-net10-self-contained-in-place-cutover.md`). Every request is a Test Call (`IsTest`): TransPlace and TransportationInsight go to their sandbox hosts, and UPS calls hit production UPS with fictitious shipments (ADR 0002).

## Files

- `requests.json` — the fixed, fictitious request set (UPS parcel, UPS ground freight and LTL, accessorials, one invalid-address case). Ship-from plants are limited to ALP, BUT, FTW and POR, the only plants the app supports (`Config._PlantCodes` / `_PlantNames`); any other plant code fails with "key not present in the dictionary".
- `Run-Parity.ps1` — `capture` saves the harness responses; `compare` re-runs the same requests (same pickup date) and diffs the logged quotes, ignoring ids and timings. It fails on a harness `processingError`, an empty call log, or any logged call not flagged `IsTest`, and lists the host each provider's calls went to so you can confirm LTL hit sandbox hosts.
- `sandbox-setup.sql` — creates the `UpsRate` database on the SANDBOX server (reference data copied from `CostPlus`).
- `results/` — saved results of a run, with the TransPlace `loginToken` redacted. Rates move, so these are a dated snapshot, not a permanent baseline: re-capture before comparing on another day.

## Procedure

1. **SANDBOX database.** Apply `sandbox-setup.sql`, then `FreightEstApp35/Sql/003_CarrierApiCallQuote.sql` and `004_CarrierApiCall_IsTest.sql` (001/002 are superseded by 003 and not needed on a fresh database). The linked-server name for the provider abbreviations is `SANDBOX` itself.
2. **Point the harness at SANDBOX.** `Config.SetProdDebug()` always uses the `PROD_*` settings, so edit the settings in the harness's *build-output* config copy (never the tracked `App.config`): `PROD_Server`, `PROD_User`, `PROD_Password` (and the `TEST_*` equivalents, to be safe) and `RemoteServerName=SANDBOX`.
3. **Baseline (net48).** Build the harness from the last net48 commit (`7f5d539`, master before the upgrade) and start it. The output config is `CarrierTestHarness.exe.config`. The old project's build output is missing four package DLLs it references (`System.Runtime.CompilerServices.Unsafe`, `Microsoft.Bcl.AsyncInterfaces`, `System.Text.Encodings.Web`, `System.Threading.Tasks.Extensions`); copy them from `packages/` next to the exe or UPS token requests fail.
   ```powershell
   .\Run-Parity.ps1 -Mode capture -OutDir .\out\net48 -Label net48
   ```
4. **Upgraded build (net10).** Stop the net48 harness (only one can hold port 5085), build the harness from the upgrade branch, and start it. The output config is `CarrierTestHarness.dll.config`.
   ```powershell
   .\Run-Parity.ps1 -Mode compare -BaselineDir .\out\net48 -OutDir .\out\net10 -Label net10
   ```
5. Run steps 3 and 4 on the same day, close together. A mismatch is reported per request with the differing quote lines.

The harness prints its connection string, including the password, to stdout at startup. Redirect its output to a file when running it unattended.

## Known sandbox behavior (identical in both builds)

- TransPlace's sandbox host returns HTTP 404 for every quote call.
- UPS rejects the 4 x 300 lb ground-freight rating in `ltl-heavy` with HTTP 400.
- UPS rejects the invalid address in `invalid-address` with HTTP 400, which is the expected error path.

## Testing the published single-file exe against the SANDBOX queue

The harness exercises the net10 code, not the published `FreightEstApp35.exe`. To test the real artifact:

1. `dotnet publish FreightEstApp35 -p:PublishProfile=SelfContainedSingleFile`, then copy `FreightEstApp35\bin\publish\` to a scratch folder (never edit the tracked output).
2. Edit the scratch copy's `FreightEstApp35.dll.config` so `RemoteServerName`, `PROD_Server` and `TEST_Server` are all `SANDBOX` (with the SANDBOX credentials). Check this before launching: the exe polls `<RemoteServerName>.CostPlus.dbo.FreightRequests` in a loop and writes results back, so a leftover production value would process the real queue.
3. Insert the fictitious rows with `queue-test-rows.sql` (UPS-only; see the header for why).
4. Run the exe with output redirected to a file (it prints its connection string at startup). Each request logs its Carrier API Calls to `UpsRate` with `IsTest = 0`, because the queue path is not a Test Call.
5. Stop the exe when the rows have `DateProcessed` and `DateRated` set. Note it will also drain any other pending SANDBOX row; check the queue first (`WHERE (DateProcessed IS NULL OR DateRated IS NULL) AND ISNULL(NumPackages,0) > 0`).

Result on 2026-10-22: the exe read `FreightEstApp35.dll.config`, connected to SANDBOX, processed all four rows, called UPS, and wrote the rates back. The saved service-level rates for rows 1-3 are identical to the net48 harness baseline; row 4 logged the same UPS 400 as the baseline's `invalid-address`. Not exercised: `VerifyAndCorrect_PO_ZipCode` (the proc exists only in production `UpsRate`; the app swallows its absence), the LTL path, and a host with no .NET installed.
