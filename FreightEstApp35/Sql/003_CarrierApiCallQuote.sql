-- Migration: CarrierApiCall / CarrierRateQuote
-- Purpose: Replace FreightEstimatorCarrierApiLog (from 002) with an insert-only, two-table
-- schema: one row per physical carrier API call, with zero or more priced quote lines per call.
-- The prior schema's upsert-by-CorrelationId silently overwrote sibling calls sharing a
-- CorrelationId (e.g. UPS + TransPlace + TransportationInsight for the same shipment request),
-- losing data needed to compare carrier fee trends. See docs/adr/0001-carrier-api-call-quote-schema.md.
-- Instructions: Apply this script manually via SSMS/sqlcmd against the target UpsRate database.
-- Note: This is a plain SQL script - no migration framework exists in this repo, and it is not
-- confirmed whether 001 or 002 has been applied anywhere, so this is a fresh forward-only
-- migration rather than an edit to either prior script.

-- Step 1: Archive the 002 table if it exists (defensive - it may never have been applied).
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FreightEstimatorCarrierApiLog' AND schema_id = SCHEMA_ID('dbo'))
   AND NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FreightEstimatorCarrierApiLog_Archive' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    ALTER TABLE dbo.FreightEstimatorCarrierApiLog RENAME TO FreightEstimatorCarrierApiLog_Archive;
END
GO

-- Step 2: CarrierApiCall - one row per physical HTTP call to a carrier API.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CarrierApiCall' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.CarrierApiCall
    (
        Id              BIGINT IDENTITY(1,1)   NOT NULL,
        CorrelationId   VARCHAR(100)           NULL,
        Application     VARCHAR(50)            NOT NULL CONSTRAINT DF_CarrierApiCall_Application DEFAULT ('FreightEstApp35'),
        ApiProvider     VARCHAR(30)            NOT NULL,   -- UPS, TransPlace, TransportationInsight
        ShippingMode    VARCHAR(10)            NOT NULL,   -- Parcel, LTL
        Operation       VARCHAR(30)            NULL,       -- Rate, Shop, Ratetimeintransit, Shoptimeintransit, Quote
        PlantCode       VARCHAR(10)            NULL,
        AccountNumber   VARCHAR(20)            NULL,
        ShipToAddress   VARCHAR(200)           NULL,
        ShipToCity      VARCHAR(200)           NULL,
        ShipToState     VARCHAR(50)            NULL,
        ShipToZip       VARCHAR(50)            NULL,
        ShipToCountry   VARCHAR(10)            NULL,
        RequestUrl      VARCHAR(500)           NULL,
        HttpStatusCode  INT                    NULL,
        IsSuccess       BIT                    NOT NULL,
        ErrorMessage    NVARCHAR(4000)         NULL,
        RequestBody     NVARCHAR(MAX)          NULL,
        ResponseBody    NVARCHAR(MAX)          NULL,
        RequestedAtUtc  DATETIME2              NOT NULL,
        RespondedAtUtc  DATETIME2              NULL,
        DurationMs      INT                    NULL,
        CreatedAtUtc    DATETIME2              NOT NULL CONSTRAINT DF_CarrierApiCall_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_CarrierApiCall PRIMARY KEY CLUSTERED (Id)
    );

    CREATE NONCLUSTERED INDEX IX_CarrierApiCall_CorrelationId
        ON dbo.CarrierApiCall (CorrelationId);

    CREATE NONCLUSTERED INDEX IX_CarrierApiCall_ApiProvider_RequestedAtUtc
        ON dbo.CarrierApiCall (ApiProvider, RequestedAtUtc)
        INCLUDE (ShippingMode, IsSuccess);

    CREATE NONCLUSTERED INDEX IX_CarrierApiCall_IsSuccess_RequestedAtUtc
        ON dbo.CarrierApiCall (IsSuccess, RequestedAtUtc);
END
GO

-- Step 3: CarrierRateQuote - one row per priced service line returned by a CarrierApiCall.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CarrierRateQuote' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.CarrierRateQuote
    (
        Id                  BIGINT IDENTITY(1,1)   NOT NULL,
        CarrierApiCallId    BIGINT                 NOT NULL,
        CarrierName         VARCHAR(100)           NULL,
        ServiceLevel        VARCHAR(100)           NULL,
        BaseCost            DECIMAL(12,2)          NULL,
        MarkupPercent       DECIMAL(9,4)           NULL,
        PerPackageCharge    DECIMAL(12,2)          NULL,
        PerShipmentCharge   DECIMAL(12,2)          NULL,
        TotalCharge         DECIMAL(12,2)          NOT NULL,
        Currency            VARCHAR(3)             NOT NULL CONSTRAINT DF_CarrierRateQuote_Currency DEFAULT ('USD'),
        TransitDays         INT                    NULL,
        CreatedAtUtc        DATETIME2              NOT NULL CONSTRAINT DF_CarrierRateQuote_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_CarrierRateQuote PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_CarrierRateQuote_CarrierApiCall FOREIGN KEY (CarrierApiCallId)
            REFERENCES dbo.CarrierApiCall (Id)
    );

    CREATE NONCLUSTERED INDEX IX_CarrierRateQuote_CarrierApiCallId
        ON dbo.CarrierRateQuote (CarrierApiCallId);

    CREATE NONCLUSTERED INDEX IX_CarrierRateQuote_CarrierName_CreatedAtUtc
        ON dbo.CarrierRateQuote (CarrierName, CreatedAtUtc)
        INCLUDE (TotalCharge, Currency);
END
GO
