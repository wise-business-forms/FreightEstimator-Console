# SANDBOX database setup

Test Mode (see `CONTEXT.md`) runs only against SANDBOX, so SANDBOX needs the same data structures as production. This folder holds the scripts for that. Nothing here changes production.

## 1. Inventory (the owner runs it; read-only)

`inventory.sql` reads system metadata only: no table rows, no changes. Run it once per database, connected to that database, and save the output:

| Run against | Save as |
|---|---|
| production `UpsRate` (azuredb01) | `prod-UpsRate.txt` |
| production `CostPlus` on the model plant's server (ALPERP) | `prod-CostPlus.txt` |
| SANDBOX `UpsRate` | `sandbox-UpsRate.txt` |
| SANDBOX `CostPlus` | `sandbox-CostPlus.txt` |

```
sqlcmd -S <server> -d <database> -E -y 0 -s "|" -i inventory.sql -o <saved-file>
```

Hand the four files back. When diffing production against SANDBOX, ignore the `server` line at the top of each file (server name and capture time always differ). A delta migration script (schema only, no data, only objects the app references plus their dependencies) is generated from the diff and reviewed before it is applied to SANDBOX. After it is applied, re-running the inventory on SANDBOX and diffing against production should show no differences for those objects.

## Scope: only what the app uses

SANDBOX only has to match the structures this app actually touches, not the rest of production. Checked against the 2026-10-22 inventories:

- **`CostPlus.dbo.FreightRequests`**: every column the app reads or writes (103, including the numbered result slots) is identical to production. No change needed.
- **`UpsRate.dbo.Plants`**: the five columns the app reads (`PlantCode`, `City`, `State`, `Zip`, `Country`) are identical. Production's extra columns (`GLCode`, `UpsShippingNumber`) and primary key are not used by the app and are not migrated.
- **`UpsRate.dbo.CarrierApiCall` and `CarrierRateQuote`**: columns identical.
- **`UpsRate.dbo.GetPlantCharges`**: production's version filters on `Active = 'Y'`, which needs `PlantCarrierCharges2017.Active`. SANDBOX had neither. `delta-UpsRate.sql` adds the column and production's procedure text.
- **`UpsRate.dbo.VerifyAndCorrect_PO_ZipCode`**: missing on SANDBOX, along with the two tables it reads (`Lookup_ZipCodeTypes`, `Lookup_ZipCodeExceptions`). `delta-UpsRate.sql` creates all three, structure only.

`delta-UpsRate.sql` is for review and is run by the owner against SANDBOX. It copies no data, so two behaviors depend on rows that SANDBOX does not have: `Active` starts `NULL` (so `GetPlantCharges` returns no rows until the rows are marked `'Y'`), and the zip lookup tables start empty (so the procedure answers `BAD_INPUT`, which the app ignores).

## 2. Access (the DBA runs it)

`grant-sandbox-access.sql` creates a login for a Windows group and grants only data read/write on `UpsRate` and `CostPlus`, plus execute on the two stored procedures. Edit the group name at the top first. It refuses to run anywhere except SANDBOX.
