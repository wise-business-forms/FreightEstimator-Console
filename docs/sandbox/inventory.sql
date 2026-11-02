-- Read-only schema inventory. Run it once per database, connected to that database, and keep the output.
-- It reads system metadata only (sys.*): no table data is selected, and it creates or changes nothing,
-- so it is safe to run against production. The result sets are ordered so output from two servers can be diffed.
--
-- Run it against: production UpsRate, production CostPlus (the model plant's, e.g. ALPERP), and the same
-- two databases on SANDBOX. See docs/sandbox/README.md.

SET NOCOUNT ON;

SELECT 'server' AS section, @@SERVERNAME AS server_name, DB_NAME() AS database_name, GETDATE() AS captured_at;

-- Tables, views, procedures, functions, triggers
SELECT 'objects' AS section, s.name AS schema_name, o.name AS object_name, o.type_desc
FROM sys.objects o
JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE o.is_ms_shipped = 0
  AND o.type IN ('U', 'V', 'P', 'FN', 'IF', 'TF', 'TR')
ORDER BY s.name, o.name;

-- Columns
SELECT 'columns' AS section, s.name AS schema_name, o.name AS object_name, c.column_id, c.name AS column_name,
       TYPE_NAME(c.user_type_id) AS data_type, c.max_length, c.precision, c.scale, c.is_nullable, c.is_identity,
       dc.definition AS default_definition
FROM sys.columns c
JOIN sys.objects o ON o.object_id = c.object_id
JOIN sys.schemas s ON s.schema_id = o.schema_id
LEFT JOIN sys.default_constraints dc ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE o.is_ms_shipped = 0 AND o.type IN ('U', 'V')
ORDER BY s.name, o.name, c.column_id;

-- Procedure and function parameters
SELECT 'parameters' AS section, s.name AS schema_name, o.name AS object_name, p.parameter_id, p.name AS parameter_name,
       TYPE_NAME(p.user_type_id) AS data_type, p.max_length, p.is_output
FROM sys.parameters p
JOIN sys.objects o ON o.object_id = p.object_id
JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE o.is_ms_shipped = 0 AND o.type IN ('P', 'FN', 'IF', 'TF')
ORDER BY s.name, o.name, p.parameter_id;

-- Indexes and keys
SELECT 'indexes' AS section, s.name AS schema_name, o.name AS object_name, i.name AS index_name, i.type_desc,
       i.is_unique, i.is_primary_key,
       STUFF((SELECT ',' + c.name + CASE WHEN ic.is_descending_key = 1 THEN ' DESC' ELSE '' END
              FROM sys.index_columns ic
              JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
              WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
              ORDER BY ic.key_ordinal
              FOR XML PATH('')), 1, 1, '') AS key_columns
FROM sys.indexes i
JOIN sys.objects o ON o.object_id = i.object_id
JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE o.is_ms_shipped = 0 AND o.type = 'U' AND i.type > 0
ORDER BY s.name, o.name, i.name;

-- Module definitions (procedures, functions, views, triggers). definition_hash makes differences easy to spot;
-- the full text is included so a delta script can be generated. Use sqlcmd -y 0 so long text is not truncated.
SELECT 'modules' AS section, s.name AS schema_name, o.name AS object_name, o.type_desc,
       HASHBYTES('SHA2_256', m.definition) AS definition_hash, m.definition
FROM sys.sql_modules m
JOIN sys.objects o ON o.object_id = m.object_id
JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE o.is_ms_shipped = 0
ORDER BY s.name, o.name;

-- What each module depends on, so a delta can include dependencies of the objects the app references.
SELECT 'dependencies' AS section, OBJECT_SCHEMA_NAME(d.referencing_id) AS referencing_schema,
       OBJECT_NAME(d.referencing_id) AS referencing_object,
       d.referenced_server_name, d.referenced_database_name, d.referenced_schema_name, d.referenced_entity_name
FROM sys.sql_expression_dependencies d
WHERE OBJECTPROPERTY(d.referencing_id, 'IsMSShipped') = 0
ORDER BY referencing_schema, referencing_object, d.referenced_entity_name;
