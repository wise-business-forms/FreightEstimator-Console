---
status: accepted
---

# UPS Test Calls hit production; only LTL Test Calls get a real sandbox

Testing the carrier logging wiring end-to-end needs to trigger real Carrier API Calls without polluting the production fee-trend data those calls write. The obvious fix — route test traffic to a sandbox — only works for two of the three providers: TransPlace and TransportationInsight already have working demo/UAT endpoints in `Config.cs`, just gated behind hardcoded `false` flags. UPS does not: the `wwwcie.ups.com` URLs present in `App.config` belong to a legacy SOAP integration this app no longer uses, and the REST Rating API `UPSRequest.cs` actually calls has exactly one OAuth client ID/secret pair, hardcoded to production, with no sandbox counterpart. Getting real UPS sandbox credentials requires WBF to register in UPS's developer portal — an external, manual step, not something to block this feature on.

So UPS Test Calls hit production UPS, same as real traffic, with `IsTest` on `CarrierApiCall` as the only signal separating them. TransPlace and TransportationInsight Test Calls route to their real sandboxes instead. This is a deliberate asymmetry: a future reader auditing why LTL test traffic never touches production while UPS test traffic does should look here rather than "fix" the inconsistency by hardcoding a fake UPS sandbox URL that would silently fail auth, or by holding up testing indefinitely on sandbox credentials that may never materialize. If WBF later obtains real UPS non-production credentials, UPS should be revisited to match the LTL behavior.
