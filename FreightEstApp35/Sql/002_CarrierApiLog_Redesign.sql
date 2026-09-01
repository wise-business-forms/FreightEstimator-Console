-- Migration: CarrierApiLog Redesign
-- Purpose: Consolidate one row per quote instead of one row per API call
-- Date: 2026-07-16
-- Instructions: Apply this script manually via SSMS/sqlcmd against the UpsRate database
-- Note: This is a plain SQL script - no automatic execution
-- Changes: API-agnostic LTL logging, removed M33 legacy code, renamed table

-- Step 1: Archive the current table
ALTER TABLE dbo.CarrierApiLog RENAME TO CarrierApiLog_Archive;

-- Step 2: Create new FreightEstimatorCarrierApiLog table with redesigned schema
CREATE TABLE dbo.FreightEstimatorCarrierApiLog (
    -- Primary Key
    Id BIGINT IDENTITY(1,1) PRIMARY KEY CLUSTERED,

    -- Core Quote Information
    CorrelationId VARCHAR(100) NULL,
    Application VARCHAR(50) NOT NULL DEFAULT 'FreightEstApp35',
    PlantCode VARCHAR(10) NULL,
    AccountNumber VARCHAR(20) NULL,
    ShipToAddress VARCHAR(200) NULL,
    ShipToCity VARCHAR(200) NULL,
    ShipToState VARCHAR(50) NULL,
    ShipToZip VARCHAR(50) NULL,
    ShipToCountry VARCHAR(10) NULL,

    -- Quote Timing
    RequestedAtUtc DATETIME2 NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATETIME(),

    -- LTL Carrier Data (API-agnostic, extensible for future logistics partners)
    LtlCarrier VARCHAR(50) NULL,  -- e.g., 'TransPlace', 'TransportationInsight', or future partner name
    LtlRequestBody NVARCHAR(MAX) NULL,
    LtlResponseBody NVARCHAR(MAX) NULL,  -- JSON array if multiple plants
    LtlHttpStatusCode INT NULL,
    LtlIsSuccess BIT NULL,
    LtlErrorMessage NVARCHAR(4000) NULL,
    LtlDurationMs INT NULL,
    LtlCompletedAtUtc DATETIME2 NULL
);

-- Step 3: Create Indexes
CREATE NONCLUSTERED INDEX IX_FreightEstimatorCarrierApiLog_CorrelationId
ON dbo.FreightEstimatorCarrierApiLog (CorrelationId)
INCLUDE (Application, LtlCarrier);

CREATE NONCLUSTERED INDEX IX_FreightEstimatorCarrierApiLog_CreatedAtUtc
ON dbo.FreightEstimatorCarrierApiLog (CreatedAtUtc)
INCLUDE (Application, CorrelationId, LtlCarrier);

CREATE NONCLUSTERED INDEX IX_FreightEstimatorCarrierApiLog_Success
ON dbo.FreightEstimatorCarrierApiLog (CreatedAtUtc, LtlIsSuccess)
INCLUDE (LtlCarrier);

-- Step 4: Verify Archive Table
-- Query to verify archive was successful:
-- SELECT COUNT(*) as ArchivedRecords FROM dbo.CarrierApiLog_Archive;

-- Step 5: Verify New Table
-- The new table is now ready for use by the application
-- SELECT COUNT(*) as NewRecords FROM dbo.FreightEstimatorCarrierApiLog;
