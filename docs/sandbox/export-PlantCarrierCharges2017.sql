-- Read-only generator. Run it against production UpsRate; it prints a SQL script (one line per row of output)
-- that loads production's PlantCarrierCharges2017 rows into SANDBOX. It selects from that one table and
-- changes nothing, so it is safe on production.
--
--   sqlcmd -S <prod server> -d UpsRate -U <sql login> -h -1 -y 0 -s "|" -i export-PlantCarrierCharges2017.sql -o load-PlantCarrierCharges2017.sql
--
-- Review load-PlantCarrierCharges2017.sql, then run it on SANDBOX in UpsRate. It refuses to run anywhere
-- else, and it REPLACES SANDBOX's existing rows in that table with production's (all in one transaction).
-- It also loads production's Active values, so GetPlantCharges (which filters Active = 'Y') returns rows.

SET NOCOUNT ON;

SELECT line
FROM (
    SELECT 0 AS part, 0 AS k, CAST(N'-- Load of production PlantCarrierCharges2017 into SANDBOX. Generated; review before running.' AS nvarchar(max)) AS line
    UNION ALL SELECT 0, 1, N'SET XACT_ABORT ON;'
    UNION ALL SELECT 0, 2, N'IF @@SERVERNAME <> ''SANDBOX'' THROW 50040, ''Refusing to run: not connected to SANDBOX.'', 1;'
    UNION ALL SELECT 0, 3, N'IF DB_NAME() <> ''UpsRate'' THROW 50041, ''Refusing to run: connect to the UpsRate database.'', 1;'
    UNION ALL SELECT 0, 4, N'IF COL_LENGTH(''dbo.PlantCarrierCharges2017'', ''Active'') IS NULL THROW 50042, ''Run delta-UpsRate.sql first: Active column is missing.'', 1;'
    UNION ALL SELECT 0, 5, N'BEGIN TRANSACTION;'
    UNION ALL SELECT 0, 6, N'DELETE FROM dbo.PlantCarrierCharges2017;'

    UNION ALL
    SELECT 1, CarrierPlantChargesId,
        N'INSERT INTO dbo.PlantCarrierCharges2017 (CarrierPlantChargesId, CarrierId, PlantCode, PerPackageCharge, PerShipmentCharge, '
        + N'NextDayAir, SecondDayAir, Ground, ThreeDaySelect, NextDayAirSaver, NextDayAirEarlyAM, SecondDayAirAM, Saver, Active) VALUES ('
        + CONVERT(nvarchar(20), CarrierPlantChargesId) + N', '
        + ISNULL(N'N''' + REPLACE(CarrierId, N'''', N'''''') + N'''', N'NULL') + N', '
        + ISNULL(N'N''' + REPLACE(PlantCode, N'''', N'''''') + N'''', N'NULL') + N', '
        + ISNULL(CONVERT(nvarchar(40), PerPackageCharge, 2), N'NULL') + N', '
        + ISNULL(CONVERT(nvarchar(40), PerShipmentCharge, 2), N'NULL') + N', '
        + ISNULL(CONVERT(nvarchar(40), NextDayAir), N'NULL') + N', '
        + ISNULL(CONVERT(nvarchar(40), SecondDayAir), N'NULL') + N', '
        + ISNULL(CONVERT(nvarchar(40), Ground), N'NULL') + N', '
        + ISNULL(CONVERT(nvarchar(40), ThreeDaySelect), N'NULL') + N', '
        + ISNULL(CONVERT(nvarchar(40), NextDayAirSaver), N'NULL') + N', '
        + ISNULL(CONVERT(nvarchar(40), NextDayAirEarlyAM), N'NULL') + N', '
        + ISNULL(CONVERT(nvarchar(40), SecondDayAirAM), N'NULL') + N', '
        + ISNULL(CONVERT(nvarchar(40), Saver), N'NULL') + N', '
        + ISNULL(N'N''' + REPLACE(Active, N'''', N'''''') + N'''', N'NULL') + N');'
    FROM dbo.PlantCarrierCharges2017

    UNION ALL SELECT 2, 0, N'COMMIT TRANSACTION;'
    UNION ALL SELECT 2, 1, N'SELECT COUNT(*) AS rows_loaded FROM dbo.PlantCarrierCharges2017;'
) x
ORDER BY part, k;
