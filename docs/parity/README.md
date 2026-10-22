# Parity check: net48 baseline vs net10 build

Proves the upgraded app produces the same Carrier Rate Quotes as the old one, using the Carrier Test Harness as the single seam (see `docs/adr/0003-net10-self-contained-in-place-cutover.md`). Every request is a Test Call (`IsTest`): TransPlace and TransportationInsight go to their sandboxes, and UPS calls hit production UPS with fictitious shipments (ADR 0002).

## Files

- `requests.json` — the fixed, fictitious request set (UPS parcel, UPS ground freight and LTL, accessorials, one error case).
- `Run-Parity.ps1` — `capture` saves the harness responses; `compare` re-runs the same requests (same pickup date) and diffs the logged quotes, ignoring ids and timings. It also fails if any logged call is not flagged `IsTest`, and lists the host each provider's calls went to so you can confirm LTL hit sandbox hosts.

## Procedure

1. **Point the harness at the SANDBOX database.** `Config.SetProdDebug()` always uses the `PROD_*` settings, so edit the settings in the harness's *build-output* config copy (never the tracked `App.config`): `PROD_Server`, `PROD_User`, `PROD_Password`, and `RemoteServerName` (the linked server that exposes `CostPlus.dbo.FreightProviderAbbreviations`). SANDBOX needs the `UpsRate` database with the `Sql/001`–`004` migrations applied.
2. **Baseline (net48).** Build the harness from the last net48 commit (`7f5d539`, master before the upgrade) and start it. The output config is `CarrierTestHarness.exe.config`.
   ```powershell
   .\Run-Parity.ps1 -Mode capture -OutDir .\out\net48 -Label net48
   ```
3. **Upgraded build (net10).** Build the harness from the upgrade branch and start it. The output config is `CarrierTestHarness.dll.config`.
   ```powershell
   .\Run-Parity.ps1 -Mode compare -BaselineDir .\out\net48 -OutDir .\out\net10 -Label net10
   ```
4. Run steps 2 and 3 on the same day, close together; carrier rates can move. A mismatch is reported per request with the differing quote lines.

The harness listens on `http://localhost:5085/` and only one instance can run at a time, so stop the net48 harness before starting the net10 one.
