-- SANDBOX delta for UpsRate, generated from the production (azuredb01) and SANDBOX inventories of 2026-10-22.
-- Schema only: no data is copied. For review before it is applied to SANDBOX.
-- Idempotent. Refuses to run anywhere except SANDBOX. ONE batch (no GO) so the guard stops everything;
-- the procedures are created through sp_executesql because CREATE PROCEDURE must be first in its own batch.
--
-- Scope is what the app uses:
--   * GetPlantCharges: production's version filters on Active = 'Y' in both queries, which needs
--     PlantCarrierCharges2017.Active. Procedure text below is production's, verbatim.
--   * VerifyAndCorrect_PO_ZipCode: missing on SANDBOX, plus the two lookup tables it reads.
--     Production's text, verbatim. The app ignores BAD_INPUT / NO_ALTERNATIVE results.

IF @@SERVERNAME <> 'SANDBOX' THROW 50030, 'Refusing to run: not connected to SANDBOX.', 1;

IF DB_NAME() <> 'UpsRate' THROW 50031, 'Refusing to run: connect to the UpsRate database.', 1;

-- PlantCarrierCharges2017.Active (production: varchar(1) NULL)
IF COL_LENGTH('dbo.PlantCarrierCharges2017', 'Active') IS NULL
    ALTER TABLE dbo.PlantCarrierCharges2017 ADD Active varchar(1) NULL;

-- Lookup tables read by VerifyAndCorrect_PO_ZipCode (structure only; they start empty)
IF OBJECT_ID('dbo.Lookup_ZipCodeTypes', 'U') IS NULL
    CREATE TABLE dbo.Lookup_ZipCodeTypes
    (
        ZipCode     varchar(50)  NULL,
        ZipCodeType varchar(50)  NULL,
        City        varchar(200) NULL,
        [State]     varchar(50)  NULL
    );

IF OBJECT_ID('dbo.Lookup_ZipCodeExceptions', 'U') IS NULL
    CREATE TABLE dbo.Lookup_ZipCodeExceptions
    (
        ZipCode   varchar(50) NOT NULL,
        DateAdded datetime    NULL
    );

-- Stored procedures: production's definitions
EXEC sp_executesql N'-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[GetPlantCharges]
	@Carrier VARCHAR(50),
	@AcctNumber INT
AS
BEGIN
	SET NOCOUNT ON;

	IF EXISTS(SELECT AccountNumber FROM PiedmontAcctsForSpecialAlpRates WHERE AccountNumber = @AcctNumber)

		SELECT	CASE WHEN PlantCode = ''ALZ'' THEN ''ALP'' ELSE PlantCode END as PlantCode,
				PerPackageCharge,
				PerShipmentCharge,
				NextDayAir,
				SecondDayAir,
				Ground,
				ThreeDaySelect,
				NextDayAirSaver,
				NextDayAirEarlyAM,
				SecondDayAirAM,
				Saver
		FROM	PlantCarrierCharges2017
		WHERE	CarrierId = @carrier
				AND	PlantCode != ''ALP''
				AND Active = ''Y''
		ORDER BY
				CASE WHEN PlantCode = ''ALZ'' THEN ''ALP'' ELSE PlantCode END

	ELSE

		SELECT	PlantCode,
				PerPackageCharge,
				PerShipmentCharge,
				NextDayAir,
				SecondDayAir,
				Ground,
				ThreeDaySelect,
				NextDayAirSaver,
				NextDayAirEarlyAM,
				SecondDayAirAM,
				Saver
		FROM	PlantCarrierCharges2017
		WHERE	CarrierId = @carrier
				AND	PlantCode != ''ALZ''
				AND Active = ''Y''
		ORDER BY
				PlantCode

END';

EXEC sp_executesql N'CREATE OR ALTER PROCEDURE [dbo].[VerifyAndCorrect_PO_ZipCode]
  ( @City varchar(200),
    @State VARCHAR(50),
    @ZipCode VARCHAR(50),
    @CorrectZipCode VARCHAR(50) OUTPUT
  )
AS

  DECLARE @ZipCodeType VARCHAR(50)

  SELECT @ZipCodeType = ZipcodeType
  FROM  Lookup_ZipCodeTypes
  WHERE /*(City = @City OR @City = '''')
  AND  ([State] = @State OR @State = '''')
  AND*/   ZipCode = @ZipCode

  IF @ZipCodeType IS NULL
    SELECT @CorrectZipCode = ''BAD_INPUT''
  ELSE
    IF @ZipCodeType = ''PO BOX''
	BEGIN
		IF EXISTS(SELECT ZipCode FROM Lookup_ZipCodeExceptions WHERE ZipCode = @ZipCode)
			SELECT @CorrectZipCode = @ZipCode
		ELSE
		  SELECT TOP 1 @CorrectZipCode = tbl2.ZipCode
		  FROM   Lookup_ZipCodeTypes tbl1,
				 Lookup_ZipCodeTypes tbl2
		  WHERE  tbl1.ZipCode = @ZipCode
		  AND   tbl1.City = tbl2.City
		  AND   tbl1.[State] = tbl2.[State]
		  AND   tbl2.ZipCodeType != ''PO BOX''
		  AND   tbl2.ZipCode != @ZipCode
		  ORDER by ABS(CONVERT(INT, @ZipCode) - CONVERT(INT, tbl2.ZipCode)) asc

	END
    ELSE
      SELECT @CorrectZipCode = @ZipCode

  IF @CorrectZipCode IS NULL
    SELECT @CorrectZipCode = ''NO_ALTERNATIVE''';
