---
status: accepted
---

# Normalize carrier logging into CarrierApiCall + CarrierRateQuote, insert-only

The carrier logging schema went through two prior iterations in this repo's history (`Sql/001_CreateCarrierApiLog.sql`, `Sql/002_CarrierApiLog_Redesign.sql`) that each collapsed toward a single flat table and an upsert-by-`CorrelationId` write pattern. That upsert silently overwrote sibling rows: a single shipment request triggers multiple carrier calls (UPS Rate, UPS Shop, TransPlace, TransportationInsight) sharing one `CorrelationId`, so only the last call's data survived per request — unacceptable for a feature whose purpose is comparing carrier fee trends over time.

We instead split logging into two insert-only tables: **`CarrierApiCall`** (one row per physical HTTP call — diagnostics: URL, status, duration, raw request/response) and **`CarrierRateQuote`** (one row per priced service line returned by a call, FK'd to it — `CarrierName`, `ServiceLevel`, and a cost breakdown of `BaseCost`/`MarkupPercent`/`PerPackageCharge`/`PerShipmentCharge`/`TotalCharge`). A single call can return several quote lines (e.g. multiple UPS service levels, or multiple LTL carriers per plant); normalizing avoids duplicating multi-KB request/response blobs once per quote line, and separates call-level reliability data from quote-level pricing data that trend queries actually need. `CorrelationId` remains the join key across every call belonging to one shipment request — no row is ever updated in place.

This is a real trade-off (two tables and a join vs. one flat table) taken deliberately after two flatter designs already proved lossy; a future reader tempted to re-flatten this should re-read this ADR first.
