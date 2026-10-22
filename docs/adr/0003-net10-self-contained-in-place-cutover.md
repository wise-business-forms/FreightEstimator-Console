---
status: accepted
---

# .NET 10 upgrade ships self-contained and replaces the running instance in place

FreightEstApp35 moves from .NET Framework 4.8 (ClickOnce-style publish to a UNC share) to .NET 10. ClickOnce is not supported for .NET 10 console apps, so deployment changes: `dotnet publish` produces a self-contained single-file build that is copied to the same share. Self-contained was chosen over framework-dependent so the production host needs no .NET runtime install and the app cannot be broken by a runtime update.

The cutover replaces the running net48 instance in place during a quiet window, with no period where both builds run. This is not just caution: `DBUtil.getNextRequestFromQueue` does a plain `SELECT TOP 1 ... WHERE DateProcessed IS NULL OR DateRated IS NULL` with no lock or in-progress marker, so two instances would pick up the same request and double-process it (double-calling carriers and double-writing Carrier API Call rows). Do not "de-risk" the cutover by running old and new side by side without first adding a claim mechanism to the queue. The last net48 publish folder is kept for rollback.

The upgrade is deliberately a behavior-preserving retarget: it stays a Windows console app with the same `App.config`-driven configuration, and hosting-model changes, secrets handling, and SQL encryption hardening are separate decisions. The legacy SOAP web references and the external `SoapTrace.dll` are removed rather than ported, because only the REST `UPSService` is still used.

Two deployment details that look wrong but are not. First, a single-file build reads its settings from `FreightEstApp35.dll.config`, not `FreightEstApp35.exe.config`; a copy named `.exe.config` is silently ignored and the app starts with null settings. Operators editing configuration on the server must edit the `.dll.config` file. Second, the publish profile bundles `Microsoft.Data.SqlClient.SNI.dll` into the exe (`IncludeNativeLibrariesForSelfExtract`); leaving it as a separate file beside the exe is fragile, because a deploy that copies only the exe and config fails on the first SQL call.

"Behavior-preserving" is meant at the level of what the app does for a shipment request, not byte-for-byte runtime behavior. The deliberate differences are: the forced TLS 1.1/1.2 setting is dropped so the OS default applies; `Encoding.Default` is replaced by `Encoding.ASCII` for the UPS credential header (identical bytes, since the credentials are pure ASCII); and the connection strings now set `Encrypt=False;TrustServerCertificate=True` explicitly, because Microsoft.Data.SqlClient defaults to `Encrypt=true` and would otherwise break connections that worked under System.Data.SqlClient. That last setting is the old behavior made explicit, not a security decision; tightening it is a separate change. Configuration is still `App.config`-driven, now read through the `System.Configuration.ConfigurationManager` package.
