---
status: accepted
---

# The environment comes from the machine name; a mismatched config refuses to start

The app used to read the same production settings everywhere: the tracked config pointed both its production and test settings at azuredb01, the "environment" label was a constant, and the host-name check that once chose between them was commented out. Testing safely meant hand-editing a build-output config before every run, and a workstation run with a copied config could read and write production data.

Now the app classifies each run from `Environment.MachineName` against a `ProductionHosts` list in its config. A machine on the list is a **Production Host**; every other machine, including every developer workstation, runs in **Test Mode**. Before any connection opens, the app checks the servers it is configured to use. In Test Mode both `PROD_Server` and `RemoteServerName` must be `SANDBOX`, and Test Mode connects with Windows Integrated Security so no SQL password is involved. A Production Host must use neither as `SANDBOX`. Any mismatch exits with a message naming the setting. The tracked default config points at SANDBOX, so a mistake lands on SANDBOX and not on production. The decision is a pure function (`EnvironmentEvaluator`) so the rules are tested without a database.

Considered and rejected: a compiled-in list of production host names (changing production servers would need a rebuild), and an explicit `Environment=PRODUCTION|SANDBOX` setting (a copied config silently carries the wrong value; the machine name is something a copied file cannot change). A machine-level environment variable would be stronger against a copied config but not editable per host without tooling, so the list lives in config.

## Consequences

- **Accepted risk:** the host list is configuration. A config copied to a workstation that lists the workstation as a production host would be treated as production. The guard prevents accidents, not deliberate misconfiguration.
- **Production deployment must set `ProductionHosts`.** A production machine missing from the list runs in Test Mode, and refuses to start if pointed at a production server. It fails loudly rather than touching the wrong data, but the runbook now makes setting it a deploy step.
- **This covers the databases only.** UPS Test Calls still reach production UPS (ADR 0002), and a Test Mode run still drains any pending SANDBOX queue row and calls UPS for it.
- **Production authentication is unchanged** (SQL login). The committed `sa` password remains in git history and must be rotated by its owner.
- **SANDBOX must mirror production's data structures** for Test Mode to exercise the same code paths; `docs/sandbox/` holds the inventory and access scripts for that.
