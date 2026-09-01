# FreightEstApp35

A console app that polls third-party carrier APIs (UPS parcel, TransPlace and TransportationInsight LTL) for shipping rates and reports back a quoted cost per shipment request.

## Language

**ApiProvider**:
The third-party service being called for a rate: `UPS`, `TransPlace`, or `TransportationInsight`. This is *who you called*, not whose price came back.
_Avoid_: Carrier (ambiguous — see CarrierName)

**CarrierName**:
The trucking/shipping company whose price a quote line represents. For LTL, this is the underlying carrier named inside the TransPlace/TransportationInsight response (e.g. an actual trucking line, or `LTL BENCHMARK`) — distinct from the ApiProvider that returned it. For UPS (parcel), ApiProvider and CarrierName collapse to the same thing.
_Avoid_: Carrier (ambiguous with ApiProvider)

**ShippingMode**:
Whether a call/quote is `Parcel` (UPS) or `LTL` (TransPlace, TransportationInsight).

**CorrelationId**:
An identifier (`source-uniqueId`) assigned once per incoming shipment request, shared across every carrier API call made while fulfilling that request. Used to group UPS and LTL calls for the same request together for comparison — it does not identify a single API call.

**Carrier API Call**:
One physical HTTP request/response to a carrier's API (one plant, one operation). Carries call-level diagnostics: URL, status code, duration, success/error, raw request/response bodies. A single shipment request produces multiple of these (e.g. one per LTL plant, one per UPS operation).

**Carrier Rate Quote**:
One priced service line returned within a single Carrier API Call (e.g. one UPS service level, or one carrier's rate in an LTL response). Carries the cost breakdown: base cost, markup, per-package/per-shipment surcharges, and the resulting total charge.

**Plant**:
A shipping origin location, identified by PlantCode, used as the ship-from address for a rate request.

**Test Call**:
A Carrier API Call deliberately triggered for testing rather than a real customer shipment, marked by the `IsTest` flag. UPS has no available sandbox for the REST Rating API this app uses, so a UPS Test Call still hits production UPS — `IsTest` is the only thing distinguishing it from real traffic. TransPlace and TransportationInsight Test Calls route to their real sandbox/demo endpoints instead of production.

**Carrier Test Harness**:
A standalone tool, separate from the always-running production request-processing service, used to trigger one shipment request on demand and inspect the resulting Carrier API Call/Carrier Rate Quote rows. Every request through it is a Test Call.
