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
sqlcmd -S <server> -d <database> -E -y 0 -W -s "|" -i inventory.sql -o <saved-file>
```

Hand the four files back. When diffing production against SANDBOX, ignore the `server` line at the top of each file (server name and capture time always differ). A delta migration script (schema only, no data, only objects the app references plus their dependencies) is generated from the diff and reviewed before it is applied to SANDBOX. After it is applied, re-running the inventory on SANDBOX and diffing against production should show no differences for those objects.

Objects the app references. In `UpsRate`: `Plants`, `GetPlantCharges` (and the tables it reads), `VerifyAndCorrect_PO_ZipCode`, `CarrierApiCall`, `CarrierRateQuote`. In `CostPlus`: `FreightRequests` and `FreightProviderAbbreviations`.

## 2. Access (the DBA runs it)

`grant-sandbox-access.sql` creates a login for a Windows group and grants only data read/write on `UpsRate` and `CostPlus`, plus execute on the two stored procedures. Edit the group name at the top first. It refuses to run anywhere except SANDBOX.
