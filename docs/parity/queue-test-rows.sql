-- Fictitious, UPS-only requests for testing the published FreightEstApp35.exe against the SANDBOX queue.
-- Run against SANDBOX (CostPlus). It refuses to run anywhere else.
--
-- UPS-only on purpose: a queue-driven request is NOT a Test Call (IsTest is false on that path), so a
-- RequestLTL request would hit the production TransportationInsight endpoint. UPS-only requests call
-- production UPS with the fictitious addresses below, like the harness does (ADR 0002).
--
-- The ship-from plant is derived from FromState (GA->ALP, PA->BUT, IN->FTW, SC->PDT, ME->POR), so the
-- From address is copied from UpsRate.dbo.Plants. DateRequested is set in the past so these rows are
-- picked up before anything else pending. Row 4 is the invalid-address error path.
--
-- Cleanup: DELETE FROM dbo.FreightRequests WHERE LoginId = 'PARITY-TEST';

IF @@SERVERNAME <> 'SANDBOX' THROW 50002, 'Refusing to insert: not connected to SANDBOX.', 1;
IF EXISTS (SELECT 1 FROM dbo.FreightRequests WHERE LoginId = 'PARITY-TEST') THROW 50003, 'PARITY-TEST rows already exist.', 1;

DECLARE @alp TABLE (a varchar(100), c varchar(50), s varchar(10), z varchar(20));
INSERT @alp SELECT Address, City, State, Zip FROM UpsRate.dbo.Plants WHERE PlantCode = 'ALP';
DECLARE @ftw TABLE (a varchar(100), c varchar(50), s varchar(10), z varchar(20));
INSERT @ftw SELECT Address, City, State, Zip FROM UpsRate.dbo.Plants WHERE PlantCode = 'FTW';
DECLARE @por TABLE (a varchar(100), c varchar(50), s varchar(10), z varchar(20));
INSERT @por SELECT Address, City, State, Zip FROM UpsRate.dbo.Plants WHERE PlantCode = 'POR';

INSERT dbo.FreightRequests (LoginId, QtyNumber, ToAddress, ToCity, ToState, ToZip, ToCountry, FromAddress, FromCity, FromState, FromZip, FromCountry,
    NumPackages, PkgWeight, LastPkgWeight, RequestUPS, RequestLTL, PickupDate, DateRequested, DateQueued, ResidentialDelivery)
SELECT 'PARITY-TEST', 1, '100 Parity Test Ln', 'Springfield', 'IL', '62701', 'US', a, c, s, z, 'US', 1, 10, 0, 1, 0, '2026-10-26', '2026-10-01', '2026-10-01', 0 FROM @alp
UNION ALL
SELECT 'PARITY-TEST', 2, '200 Parity Test Ave', 'Dallas', 'TX', '75201', 'US', a, c, s, z, 'US', 5, 40, 25, 1, 0, '2026-10-26', '2026-10-01', '2026-10-01', 0 FROM @ftw
UNION ALL
SELECT 'PARITY-TEST', 3, '300 Parity Test Ct', 'Portland', 'OR', '97201', 'US', a, c, s, z, 'US', 2, 15, 0, 1, 0, '2026-10-26', '2026-10-01', '2026-10-01', 1 FROM @por
UNION ALL
SELECT 'PARITY-TEST', 4, '0 Nowhere Rd', 'Faketown', 'ZZ', '00000', 'US', a, c, s, z, 'US', 1, 10, 0, 1, 0, '2026-10-26', '2026-10-01', '2026-10-01', 0 FROM @alp;
