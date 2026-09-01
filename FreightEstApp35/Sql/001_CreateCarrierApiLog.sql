-- Creates the structured carrier request/response log table in the UpsRate database.
-- This is a plain SQL script (no migration framework exists in this repo) - apply manually
-- via SSMS/sqlcmd against the target UpsRate database. Not executed automatically.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CarrierApiLog' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.CarrierApiLog
    (
        Id              BIGINT IDENTITY(1,1)   NOT NULL,
        CorrelationId   VARCHAR(100)           NULL,
        Application     VARCHAR(50)            NOT NULL,
        Carrier         VARCHAR(30)            NOT NULL,
        Operation       VARCHAR(30)            NULL,
        PlantCode       VARCHAR(10)            NULL,
        AccountNumber   VARCHAR(20)            NULL,
        ShipToAddress   VARCHAR(200)           NULL,
        ShipToCity      VARCHAR(200)           NULL,
        ShipToState     VARCHAR(50)            NULL,
        ShipToZip       VARCHAR(50)            NULL,
        ShipToCountry   VARCHAR(10)            NULL,
        RequestUrl      VARCHAR(500)           NULL,
        HttpMethod      VARCHAR(10)            NULL,
        HttpStatusCode  INT                    NULL,
        IsSuccess       BIT                    NOT NULL,
        ErrorMessage    NVARCHAR(4000)         NULL,
        RequestBody     NVARCHAR(MAX)          NULL,
        ResponseBody    NVARCHAR(MAX)          NULL,
        RequestedAtUtc  DATETIME2              NOT NULL,
        RespondedAtUtc  DATETIME2              NULL,
        DurationMs      INT                    NULL,
        UserName        VARCHAR(50)            NULL,
        CreatedAtUtc    DATETIME2              NOT NULL CONSTRAINT DF_CarrierApiLog_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_CarrierApiLog PRIMARY KEY CLUSTERED (Id)
    );

    CREATE NONCLUSTERED INDEX IX_CarrierApiLog_CorrelationId
        ON dbo.CarrierApiLog (CorrelationId);

    CREATE NONCLUSTERED INDEX IX_CarrierApiLog_Carrier_RequestedAtUtc
        ON dbo.CarrierApiLog (Carrier, RequestedAtUtc);

    CREATE NONCLUSTERED INDEX IX_CarrierApiLog_IsSuccess_RequestedAtUtc
        ON dbo.CarrierApiLog (IsSuccess, RequestedAtUtc);
END
GO
