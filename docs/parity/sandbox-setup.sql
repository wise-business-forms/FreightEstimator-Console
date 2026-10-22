-- Sets up an UpsRate database on the SANDBOX server so the Carrier Test Harness can run there.
-- Idempotent and additive: creates UpsRate if missing and copies reference data from CostPlus
-- into it. It never modifies CostPlus or any other existing database.
--
-- Apply this first, then Sql/003_CarrierApiCallQuote.sql and Sql/004_CarrierApiCall_IsTest.sql
-- (001 and 002 are superseded by 003 and are not needed on a fresh database).
--
-- Also needs, already present on SANDBOX: CostPlus.dbo.FreightProviderAbbreviations, read through
-- the RemoteServerName setting (set it to SANDBOX).

IF DB_ID('UpsRate') IS NULL
    CREATE DATABASE UpsRate;
GO

USE UpsRate;
GO

-- Reference data the rating path reads: plants and the per-plant carrier charges.
IF OBJECT_ID('dbo.Plants', 'U') IS NULL
    SELECT * INTO dbo.Plants FROM CostPlus.dbo.Plants;

IF OBJECT_ID('dbo.PlantCarrierCharges2017', 'U') IS NULL
    SELECT * INTO dbo.PlantCarrierCharges2017 FROM CostPlus.dbo.PlantCarrierCharges2017;

IF OBJECT_ID('dbo.PiedmontAcctsForSpecialAlpRates', 'U') IS NULL
    SELECT * INTO dbo.PiedmontAcctsForSpecialAlpRates FROM CostPlus.dbo.PiedmontAcctsForSpecialAlpRates;
GO

-- GetPlantCharges, copied verbatim from CostPlus so it resolves tables in this database.
IF OBJECT_ID('dbo.GetPlantCharges', 'P') IS NULL
BEGIN
    -- (OBJECT_DEFINITION resolves ids in the current database, so read sys.sql_modules in CostPlus.)
    DECLARE @definition NVARCHAR(MAX) =
        (SELECT definition FROM CostPlus.sys.sql_modules WHERE object_id = OBJECT_ID('CostPlus.dbo.GetPlantCharges'));
    IF @definition IS NULL
        THROW 50001, 'CostPlus.dbo.GetPlantCharges was not found; cannot copy it into UpsRate.', 1;
    EXEC sp_executesql @definition;
END
GO
