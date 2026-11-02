-- Grants a Windows group the minimum rights the app needs on SANDBOX, so developers can run it in Test Mode
-- with Integrated Security. For the DBA to review and run; the app never runs this.
-- Idempotent. Refuses to run anywhere except SANDBOX. It is deliberately ONE batch (no GO): a THROW only ends
-- its own batch, so a GO after the guard would let the rest run on the wrong server.
--
-- Rights: read and write data in UpsRate and CostPlus, and execute the stored procedures the app calls.
-- No sysadmin, db_owner, DDL, or database-creation rights.

IF @@SERVERNAME <> 'SANDBOX' THROW 50010, 'Refusing to run: not connected to SANDBOX.', 1;

DECLARE @GroupLogin sysname = N'WISENT\FreightEstimator-Developers';  -- edit: the AD group to grant

IF SUSER_ID(@GroupLogin) IS NULL
    EXEC (N'CREATE LOGIN ' + QUOTENAME(@GroupLogin) + N' FROM WINDOWS');

DECLARE @db sysname, @sql nvarchar(max);
DECLARE dbs CURSOR LOCAL FAST_FORWARD FOR SELECT name FROM sys.databases WHERE name IN (N'UpsRate', N'CostPlus');
OPEN dbs;
FETCH NEXT FROM dbs INTO @db;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'USE ' + QUOTENAME(@db) + N';
    IF DATABASE_PRINCIPAL_ID(@g) IS NULL EXEC (N''CREATE USER '' + QUOTENAME(@g) + N'' FOR LOGIN '' + QUOTENAME(@g));
    EXEC (N''ALTER ROLE db_datareader ADD MEMBER '' + QUOTENAME(@g));
    EXEC (N''ALTER ROLE db_datawriter ADD MEMBER '' + QUOTENAME(@g));';
    EXEC sp_executesql @sql, N'@g sysname', @g = @GroupLogin;

    -- Stored procedures the app calls (UpsRate only). Skipped, with a notice, if one is not there yet.
    IF @db = N'UpsRate'
    BEGIN
        SET @sql = N'USE ' + QUOTENAME(@db) + N';
        IF OBJECT_ID(N''dbo.GetPlantCharges'', ''P'') IS NOT NULL
            EXEC (N''GRANT EXECUTE ON OBJECT::dbo.GetPlantCharges TO '' + QUOTENAME(@g));
        ELSE PRINT ''dbo.GetPlantCharges not found; apply the SANDBOX migration, then re-run.'';
        IF OBJECT_ID(N''dbo.VerifyAndCorrect_PO_ZipCode'', ''P'') IS NOT NULL
            EXEC (N''GRANT EXECUTE ON OBJECT::dbo.VerifyAndCorrect_PO_ZipCode TO '' + QUOTENAME(@g));
        ELSE PRINT ''dbo.VerifyAndCorrect_PO_ZipCode not found; apply the SANDBOX migration, then re-run.'';';
        EXEC sp_executesql @sql, N'@g sysname', @g = @GroupLogin;
    END

    FETCH NEXT FROM dbs INTO @db;
END
CLOSE dbs;
DEALLOCATE dbs;
