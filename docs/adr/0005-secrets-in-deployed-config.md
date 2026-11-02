---
status: accepted
---

# Carrier and database secrets live in the deployed config, never in source

Carrier credentials (UPS, M33, TransPlace, TransportationInsight) were string literals in the application's configuration class, and the SQL `sa` password was in the tracked `App.config` files. Anyone with repo access held production credentials, and they are in git history.

The app now reads every secret from `appSettings`. The tracked `App.config` files carry no secrets and merge a git-ignored `secrets.config` that sits beside the exe (`<appSettings file="secrets.config">`); a deployed host may instead put the same keys directly in its `.dll.config`. `secrets.config.example` lists every key. At startup the app checks the required keys and refuses to start, naming each missing one, before connecting to anything. Test Mode and Production Hosts use the same mechanism, so a developer workstation needs its own `secrets.config`. The `FreightEstApp35 - ORIGINAL` folder, which held a second copy of the old code and credentials, was deleted.

Considered and rejected: environment variables (the service launcher on each host would have to set them, and they are readable by anything that can read the process environment, with no gain over a permissioned file) and a DPAPI-encrypted config section (per-machine encryption adds tooling to every cutover). The deployed config file is what the runbook already has each host carry, so it adds no new mechanism.

## Consequences

- **The old values are compromised.** They remain in git history and must be rotated by their owners: the SQL `sa` password, the UPS credentials and client secret, and the M33, TransPlace and TransportationInsight tokens and passwords. This change stops new exposure; it does not undo the old one.
- **Production hosts must have the keys before this build is deployed**, or the app refuses to start. The cutover runbook lists it as a deploy step.
- **Plaintext on disk remains.** `secrets.config` is unencrypted on the server; protect it with file permissions.
- **Database credentials are unchanged in shape:** a Production Host still takes `PROD_User` and `PROD_Password` from its deployed config; Test Mode uses Integrated Security and needs none (ADR 0004).
