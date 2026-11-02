# Cutover runbook: FreightEstApp35 net48 to .NET 10

Replaces the running .NET Framework 4.8 instance of FreightEstApp35 with the self-contained .NET 10 build, in a quiet window, and describes how to roll back. The decisions behind this are in `docs/adr/0003-net10-self-contained-in-place-cutover.md`. A person performs this; nothing here is automated.

## Two rules that decide everything

1. **Never run two instances against the same queue.** The queue has no claim step (`SELECT TOP 1 ... WHERE DateProcessed IS NULL OR DateRated IS NULL`), so two instances pick up the same request and double-process it: double carrier calls and double-written Carrier API Call rows. The old instance must be fully stopped before the new one starts, and the same applies in reverse on rollback.
2. **Settings live in `FreightEstApp35.dll.config`, not `FreightEstApp35.exe.config`.** A single-file exe ignores a `.exe.config`; it then starts with null settings. The new config file must be named `.dll.config` and sit next to the exe.

## Open items to confirm before using this runbook

The repo does not record how production is actually hosted. Fill these in, once, and keep this section current:

- [ ] **Host and folder.** The old ClickOnce publish target was `\\azweb10\c$\Wise Utilities\FreightEstApp-PTLERP\`. Confirm where the app actually runs from today.
- [ ] **How it is started and stopped** (Windows service wrapper, Task Scheduler, a console left open, or something else), and who has rights to do so.
- [ ] **How many instances exist.** The folder name says `PTLERP`, but the checked-in `App.config` has `RemoteServerName=ALPERP`, and SANDBOX has linked servers for ALPERP, BUTERP, FTWERP and PTLERP. If there is one instance per ERP, each has its own deployed config and each needs this procedure, one at a time. Never assume the repo's `App.config` matches what is deployed.
- [ ] **Where its output goes.** The app prints a startup banner to stdout: plant, SQL servers and login, connectivity checks, Windows account, runtime. It does not print the SQL password. Use the banner to verify the new instance after starting it.
- [ ] **Who watches the queue afterwards**, and how long the verification window is.

## Before the window

- [ ] The parity gate (#11) has passed: `docs/parity/README.md`, including the SANDBOX test of the published exe. Re-run the harness comparison on the day if the build changed since the last run.
- [ ] The upgrade branch is merged and the exact commit to deploy is recorded here: `__________`.
- [ ] **Carrier secrets.** The build no longer contains any credentials: the app refuses to start unless the UPS, M33, TransPlace and TransportationInsight keys listed in `FreightEstApp35/secrets.config.example` are set. Create `secrets.config` from that template beside the exe on the production host (or add the same keys to the deployed `.dll.config`), taking the values from the current production deployment. Restrict file permissions to the service account. Never commit it (ADR 0005).
- [ ] Build from that commit: `dotnet publish FreightEstApp35 -p:PublishProfile=SelfContainedSingleFile`. Output: `FreightEstApp35\bin\publish\` with `FreightEstApp35.exe` and `FreightEstApp35.dll.config`.
- [ ] Prepare the new config: take the **currently deployed** config's `appSettings` values (production server, user, password, `RemoteServerName`, log file name) and add `ProductionHosts` set to this machine's name (see `docs/adr/0004-environment-from-machine-name.md`; without it the app runs in Test Mode and refuses production servers) and put them in the new `FreightEstApp35.dll.config`. Do not ship the checked-in `App.config` values blindly. Verify every server-like setting names the intended production server.
- [ ] Record the queue state, using the production `RemoteServerName` (shown here as `<erp>`):
  ```sql
  SELECT COUNT(*) AS pending
  FROM <erp>.CostPlus.dbo.FreightRequests
  WHERE (DateProcessed IS NULL OR DateRated IS NULL) AND ISNULL(NumPackages,0) > 0;

  SELECT MAX(DateProcessed) AS last_processed FROM <erp>.CostPlus.dbo.FreightRequests;
  ```
- [ ] Record the latest Carrier API Call so you can tell new rows from old:
  ```sql
  SELECT MAX(Id) AS last_call_id FROM UpsRate.dbo.CarrierApiCall;
  ```

## Cutover

1. **Stop the old instance.** Confirm the process is gone (Task Manager or `Get-Process FreightEstApp35`), not just that its window closed. A request being processed at that moment is not lost: it is only marked processed when its results are saved, so the new instance picks it up again. At most one request is carried out twice.
2. **Keep the rollback copy.** Copy the entire current deployment folder to a dated folder beside it, for example `FreightEstApp-PTLERP.net48-YYYYMMDD`, before changing anything. It must contain the old exe, its `.exe.config`, and the old binaries. Do not delete it until the rollback window has passed.
3. **Deploy.** Copy `FreightEstApp35.exe`, the prepared `FreightEstApp35.dll.config` and `secrets.config` into the deployment folder. The `.pdb` is optional. Leave the old net48 files alone, or move them into the rollback folder.
4. **Start the new instance** the way the old one was started. Redirect or limit its output if stdout is captured (see open items).
5. **Check the startup output.** The banner must show `Mode: PRODUCTION HOST` and a `Why` line saying this machine is a configured production host. If it says `TEST MODE`, or the app printed `REFUSING TO START`, the machine's name is missing from `ProductionHosts` in the deployed config (or a server setting is wrong): stop and fix the config. Confirm the `Carrier log DB` and `Request queue` lines name the intended production servers, not SANDBOX (a Production Host refuses SANDBOX). Then it should either report requests being processed or sit idle polling. No exception, and no `TypeInitializationException` (that means the SQL native library is missing from the exe).

## Verify

Within the first few minutes:

- [ ] A real request is picked up and processed: the console shows `Results saved to DB -- source: ... id: ...`, and the row has `DateProcessed` and `DateRated` set.
- [ ] New Carrier API Call rows are being written, flagged as real traffic:
  ```sql
  SELECT ApiProvider, ShippingMode, IsSuccess, IsTest, COUNT(*) AS calls
  FROM UpsRate.dbo.CarrierApiCall
  WHERE Id > <last_call_id>
  GROUP BY ApiProvider, ShippingMode, IsSuccess, IsTest;
  ```
  Expect `IsTest = 0`. Failures are possible (carriers fail), but compare the failure rate with before the cutover.
- [ ] Priced results look right: spot-check one processed request's rates in the queue row against what the old build returned for a comparable shipment.
- [ ] The pending count from before is draining, not growing.
- [ ] Watch for requests written back with an error code (`ErrorCode01` / `ErrorDesc01`) that you did not see before.

## Roll back

Roll back if the new instance will not start, will not read its config, cannot reach SQL, or processes requests visibly wrongly (errors where there were none, rates that disagree with the old build).

1. **Stop the new instance and confirm it is gone.** Never run both.
2. Restore the rollback copy: put the contents of the dated folder back as the deployment folder (or point the start mechanism back at it).
3. Start the old instance the way it was started before.
4. Re-run the verification steps above for the old build.

The queue and logging schema are unchanged by the upgrade (the upgrade added no migrations), so no database rollback is needed. Requests processed by the new build keep their results.

## After the window

- [ ] Note the outcome and any surprises on issue #3.
- [ ] Keep the rollback folder until you are confident, then remove it.
- [ ] If anything in this runbook was wrong or incomplete, fix it here.
