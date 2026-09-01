-- Migration: CarrierApiCall.IsTest
-- Purpose: Distinguish deliberate Test Calls (triggered via the Carrier Test Harness) from real
-- shipment requests, so trend queries can exclude them. UPS has no available sandbox for the
-- REST Rating API this app uses, so UPS Test Calls still hit production - IsTest is the only
-- signal separating them. See docs/adr/0002-ups-test-calls-hit-production.md.
-- Instructions: Apply this script manually via SSMS/sqlcmd against the target UpsRate database.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.CarrierApiCall') AND name = 'IsTest'
)
BEGIN
    ALTER TABLE dbo.CarrierApiCall
        ADD IsTest BIT NOT NULL CONSTRAINT DF_CarrierApiCall_IsTest DEFAULT (0);

    CREATE NONCLUSTERED INDEX IX_CarrierApiCall_IsTest_RequestedAtUtc
        ON dbo.CarrierApiCall (IsTest, RequestedAtUtc);
END
GO
