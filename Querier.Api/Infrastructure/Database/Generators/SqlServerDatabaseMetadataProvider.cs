using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Querier.Api.Application.DTOs;
using Querier.Api.Domain.Common.Enums;
using Querier.Api.Domain.Entities.QDBConnection;
using Querier.Api.Domain.Services;
using Querier.Api.Infrastructure.Database.Templates;

namespace Querier.Api.Infrastructure.Database.Generators;

public class SqlServerDatabaseMetadataProvider : DatabaseMetadataProviderBase, IDatabaseMetadataProvider
{
    private readonly ILogger _logger;
    private readonly DatabaseModel _model;
    public SqlServerDatabaseMetadataProvider(DatabaseModel model, ILogger logger) : base()
    {
        _logger = logger;
        _model = model;
    }
    
    public List<StoredProcedureMetadata> ExtractStoredProcedureMetadata(string connectionString)
    {
        
        List<StoredProcedureMetadata> result = [];
        List<StoredProcedureMetadata> failed = [];

        using var connection = new SqlConnection(connectionString);
        connection.Open();
        
        using var listProceduresCommand = connection.CreateCommand();
        listProceduresCommand.CommandText = @"
                    SELECT * 
                      FROM INFORMATION_SCHEMA.ROUTINES
                     WHERE ROUTINE_TYPE = 'PROCEDURE' 
                       AND LEFT(ROUTINE_NAME, 3) NOT IN ('sp_', 'xp_', 'ms_')
                     ORDER BY ROUTINE_NAME";

        using var procedureReader = listProceduresCommand.ExecuteReader();
        while (procedureReader.Read())
        {
            string schemaName = (string)procedureReader["ROUTINE_SCHEMA"];
            string procedureName = (string)procedureReader["ROUTINE_NAME"];
            _logger.LogDebug("Processing stored procedure: {ProcedureName}", procedureName);
                    
            StoredProcedureMetadata procedure = new StoredProcedureMetadata
            {
                Schema = schemaName,
                Name = procedureName,
                CSName = NormalizeCsString(procedureName),
                Parameters = [],
                OutputSet = []
            };

            bool parametersMetadataDefined = false;
            bool outputSetMetadataDefined = false;

            try
            {
                procedure.Parameters = GetProcedureParametersMetadata(connection, schemaName, procedureName);
                parametersMetadataDefined = true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to get parameters metadata for procedure {procedure.Name}", procedure.Name);
                parametersMetadataDefined = false;
            }

            try
            {
                procedure.OutputSet = GetProcedureOutputMetadata(connection, schemaName, procedureName);
                outputSetMetadataDefined = procedure.OutputSet.Count > 0;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to get output set metadata for procedure {procedure.Name}", procedure.Name);
                outputSetMetadataDefined = false;
            }
            
            if (parametersMetadataDefined && outputSetMetadataDefined)
                result.Add(procedure);
            else
            {
                failed.Add(procedure);
                // AI Analysis
                /*
                SELECT 
                    referenced_schema_name, 
                    referenced_entity_name, 
                    referenced_minor_name, 
                    referenced_class_desc 
                FROM sys.dm_sql_referenced_entities('dbo.ALERT_TRANSCODING', 'OBJECT');
                */
            }
        }
        
        if (_model != null)
        {
            foreach (var procedure in failed)
            {
                _logger.LogWarning("Failed to extract metadata for procedure {ProcedureName}", procedure.Name);
                using var listDependenciesCommand = connection.CreateCommand();
                listDependenciesCommand.CommandText = @"
                    SELECT 
                    referenced_schema_name, 
                    referenced_entity_name, 
                    referenced_minor_name, 
                    referenced_class_desc 
                FROM sys.dm_sql_referenced_entities('dbo." + procedure.Name + "', 'OBJECT');";

                using var dependenciesReader = listDependenciesCommand.ExecuteReader();
                while (dependenciesReader.Read())
                {

                }
            }
        }
        

        return result;
    }
    private List<TemplateProperty> GetProcedureParametersMetadata(SqlConnection connection, string schemaName, string procedureName)
    {
        List<TemplateProperty> result = [];

        using var listParametersCommand = connection.CreateCommand();
        listParametersCommand.CommandText = @"
                    SELECT  
                           'Parameter' = name,  
                           'Type'   = type_name(user_type_id),  
                           'Length'   = max_length,  
                           'Precision'   = case when type_name(system_type_id) = 'uniqueidentifier' 
                                      then precision  
                                      else OdbcPrec(system_type_id, max_length, precision) end,  
                           'Scale'   = OdbcScale(system_type_id, scale),  
                           'Order'  = parameter_id,  
                           'Collation'   = convert(sysname, 
                                           case when system_type_id in (35, 99, 167, 175, 231, 239)  
                                           then ServerProperty('collation') end),
                            is_Output = CAST(is_Output AS INT),
                            is_nullable = CAST(is_nullable AS INT)
                      FROM sys.parameters where object_id = object_id('" + schemaName + "." + procedureName + @"')
                     ORDER BY parameter_id";
            
        listParametersCommand.Parameters.Add(new SqlParameter("@procName", procedureName));
        using var parameterReader = listParametersCommand.ExecuteReader();
        while (parameterReader.Read())
        {
            var parameter = new TemplateProperty()
            {
                Name = (string)parameterReader["Parameter"],
                CSName = NormalizeCsString((string)parameterReader["Parameter"]),
                IsKey = false,
                IsForeignKey = false,
                IsRequired = true,
                IsAutoGenerated = false,
                CSType = SqlTypeToCsType((string)parameterReader["Type"], true)
            };

            result.Add(parameter);
            _logger.LogDebug("Added parameter {ParameterName} to procedure {ProcedureName}", parameter.Name, procedureName);
        }
        
        return result;
    }
    private List<TemplateProperty> GetProcedureOutputMetadata(SqlConnection connection, string schemaName, string procedureName)
    {
        List<TemplateProperty> result = [];
        
        using var getProcedureOutput = new SqlCommand("sp_describe_first_result_set", connection);
        getProcedureOutput.CommandType = CommandType.StoredProcedure;
        getProcedureOutput.Parameters.AddWithValue("@tsql", $"EXEC [{schemaName}].[{procedureName}]");

        using var da = new SqlDataAdapter(getProcedureOutput);
        var dt = new DataTable();
        da.Fill(dt);

        foreach (DataRow row in dt.Rows)
        {
            var outputSet = new TemplateProperty()
            {
                Name = (string)row["name"],
                CSName = NormalizeCsString((string)row["name"]),
                IsKey = false,
                IsForeignKey = false,
                IsRequired = true,
                IsAutoGenerated = false,
                CSType = SqlTypeToCsType((string)row["system_type_name"], false)
            };
            result.Add(outputSet);
            _logger.LogDebug("Added output {OutputName} to procedure {ProcedureName}", outputSet.Name, procedureName);
        }
        
        return result;
    }

    private async Task<DBConnectionDatabaseSchemaDto> GetSqlServerSchema(SqlConnection connection)
    {
        DBConnectionDatabaseSchemaDto response = new DBConnectionDatabaseSchemaDto();
        try
        {

            _logger.LogTrace("Extracting tables");
            await ExtractTables(connection, response);

            _logger.LogTrace("Extracting views");
            await ExtractViews(connection, response);

            _logger.LogTrace("Extracting stored procedures");
            await ExtractStoredProcedures(connection, response);

            _logger.LogTrace("Extracting user functions");
            await ExtractUserFunctions(connection, response);

            _logger.LogDebug("Successfully extracted SQL Server schema");
            return response;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error occurred while extracting SQL Server schema");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting SQL Server schema");
            throw;
        }
    }

    private async Task ExtractTables(SqlConnection connection, DBConnectionDatabaseSchemaDto response)
    {
        try
        {
            _logger.LogDebug("Starting table extraction");
            var tableQuery = @"
                SELECT 
                    t.TABLE_SCHEMA,
                    t.TABLE_NAME,
                    c.COLUMN_NAME,
                    c.DATA_TYPE,
                    c.IS_NULLABLE,
                    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as IS_PRIMARY_KEY,
                    CASE WHEN fk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as IS_FOREIGN_KEY,
                    fk.REFERENCED_TABLE_NAME,
                    fk.REFERENCED_COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLES t
                INNER JOIN INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME AND t.TABLE_SCHEMA = c.TABLE_SCHEMA
                LEFT JOIN (
                    SELECT ku.TABLE_CATALOG,ku.TABLE_SCHEMA,ku.TABLE_NAME,ku.COLUMN_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS tc
                    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS ku
                        ON tc.CONSTRAINT_TYPE = 'PRIMARY KEY' 
                        AND tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                ) pk ON c.TABLE_NAME = pk.TABLE_NAME AND c.COLUMN_NAME = pk.COLUMN_NAME
                LEFT JOIN (
                    SELECT 
                        cu.TABLE_CATALOG,cu.TABLE_SCHEMA,cu.TABLE_NAME,cu.COLUMN_NAME,
                        cu2.TABLE_NAME as REFERENCED_TABLE_NAME, cu2.COLUMN_NAME as REFERENCED_COLUMN_NAME
                    FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
                    INNER JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cu ON rc.CONSTRAINT_NAME = cu.CONSTRAINT_NAME
                    INNER JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cu2 ON rc.UNIQUE_CONSTRAINT_NAME = cu2.CONSTRAINT_NAME
                ) fk ON c.TABLE_NAME = fk.TABLE_NAME AND c.COLUMN_NAME = fk.COLUMN_NAME
                WHERE t.TABLE_TYPE = 'BASE TABLE'
                ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME, c.ORDINAL_POSITION";

            using var command = new SqlCommand(tableQuery, connection);
            using var reader = await command.ExecuteReaderAsync();

            DBConnectionTableDescriptionDto currentTable = null;
            string currentTableName = null;
            string currentSchema = null;
            int tableCount = 0;
            int columnCount = 0;

            while (await reader.ReadAsync())
            {
                try
                {
                    var schema = reader.GetString(0);
                    var tableName = reader.GetString(1);

                    if (currentTableName != tableName || currentSchema != schema)
                    {
                        currentTable = new DBConnectionTableDescriptionDto
                        {
                            Name = tableName,
                            Schema = schema
                        };
                        response.Tables.Add(currentTable);
                        currentTableName = tableName;
                        currentSchema = schema;
                        tableCount++;
                        _logger.LogTrace("Processing table {Schema}.{Table}", schema, tableName);
                    }

                    currentTable.Columns.Add(new DBConnectionColumnDescriptionDto
                    {
                        Name = reader.GetString(2),
                        DataType = reader.GetString(3),
                        IsNullable = reader.GetString(4) == "YES",
                        IsPrimaryKey = reader.GetInt32(5) == 1,
                        IsForeignKey = reader.GetInt32(6) == 1,
                        ForeignKeyTable = !reader.IsDBNull(7) ? reader.GetString(7) : null,
                        ForeignKeyColumn = !reader.IsDBNull(8) ? reader.GetString(8) : null
                    });
                    columnCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing table row for {Table}", currentTableName);
                }
            }

            _logger.LogDebug("Extracted {TableCount} tables with {ColumnCount} total columns", tableCount, columnCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting tables");
            throw;
        }
    }

    private async Task ExtractViews(SqlConnection connection, DBConnectionDatabaseSchemaDto response)
    {
        try
        {
            _logger.LogDebug("Starting view extraction");
            var viewQuery = @"
                SELECT 
                    v.TABLE_SCHEMA,
                    v.TABLE_NAME,
                    c.COLUMN_NAME,
                    c.DATA_TYPE,
                    c.IS_NULLABLE
                FROM INFORMATION_SCHEMA.VIEWS v
                INNER JOIN INFORMATION_SCHEMA.COLUMNS c ON v.TABLE_NAME = c.TABLE_NAME AND v.TABLE_SCHEMA = c.TABLE_SCHEMA
                ORDER BY v.TABLE_SCHEMA, v.TABLE_NAME, c.ORDINAL_POSITION";

            using var command = new SqlCommand(viewQuery, connection);
            using var reader = await command.ExecuteReaderAsync();

            DBConnectionViewDescriptionDto currentView = null;
            string currentViewName = null;
            string currentSchema = null;
            int viewCount = 0;
            int columnCount = 0;

            while (await reader.ReadAsync())
            {
                try
                {
                    var schema = reader.GetString(0);
                    var viewName = reader.GetString(1);

                    if (currentViewName != viewName || currentSchema != schema)
                    {
                        currentView = new DBConnectionViewDescriptionDto
                        {
                            Name = viewName,
                            Schema = schema
                        };
                        response.Views.Add(currentView);
                        currentViewName = viewName;
                        currentSchema = schema;
                        viewCount++;
                        _logger.LogTrace("Processing view {Schema}.{View}", schema, viewName);
                    }

                    currentView.Columns.Add(new DBConnectionColumnDescriptionDto
                    {
                        Name = reader.GetString(2),
                        DataType = reader.GetString(3),
                        IsNullable = reader.GetString(4) == "YES"
                    });
                    columnCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing view row for {View}", currentViewName);
                }
            }

            _logger.LogDebug("Extracted {ViewCount} views with {ColumnCount} total columns", viewCount, columnCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting views");
            throw;
        }
    }

    private async Task ExtractStoredProcedures(SqlConnection connection, DBConnectionDatabaseSchemaDto response)
    {
        try
        {
            _logger.LogDebug("Starting stored procedure extraction");
            var spQuery = @"
                    SELECT 
                        SPECIFIC_SCHEMA,
                        SPECIFIC_NAME,
                        PARAMETER_NAME,
                        DATA_TYPE,
                        PARAMETER_MODE
                    FROM INFORMATION_SCHEMA.PARAMETERS
                    WHERE SPECIFIC_SCHEMA != 'sys'
                    ORDER BY SPECIFIC_SCHEMA, SPECIFIC_NAME, ORDINAL_POSITION";

            await using var command = new SqlCommand(spQuery, connection);
            await using var reader = await command.ExecuteReaderAsync();

            DbConnectionStoredProcedureDescriptionDto currentSp = null;
            string currentSpName = null;
            string currentSchema = null;
            int spCount = 0;
            int paramCount = 0;

            while (await reader.ReadAsync())
            {
                try
                {
                    var schema = reader.GetString(0);
                    var spName = reader.GetString(1);

                    if (currentSpName != spName || currentSchema != schema)
                    {
                        currentSp = new DbConnectionStoredProcedureDescriptionDto
                        {
                            Name = spName,
                            Schema = schema
                        };
                        response.StoredProcedures.Add(currentSp);
                        currentSpName = spName;
                        currentSchema = schema;
                        spCount++;
                        _logger.LogTrace("Processing stored procedure {Schema}.{Procedure}", schema, spName);
                    }

                    if (reader.IsDBNull(2)) continue; // Skip return value parameter
                    currentSp.Parameters.Add(new DBConnectionParameterDescriptionDto
                    {
                        Name = reader.GetString(2),
                        DataType = reader.GetString(3),
                        Mode = reader.GetString(4)
                    });
                    paramCount++;

                    await using var getProcedureOutput = new SqlCommand("sp_describe_first_result_set", connection);
                    getProcedureOutput.CommandType = CommandType.StoredProcedure;
                    getProcedureOutput.Parameters.AddWithValue("@tsql", $"EXEC [{schema}].[{spName}]");

                    using var da = new SqlDataAdapter(getProcedureOutput);
                    var dt = new DataTable();
                    da.Fill(dt);

                    foreach (DataRow row in dt.Rows)
                    {
                        try
                        {
                            var outputColumn = new DBConnectionColumnDescriptionDto
                            {
                                Name = (string)row["name"],
                                IsNullable = Convert.ToInt32(row["is_nullable"]) == 1,
                                DataType = (string)row["system_type_name"]
                            };

                            currentSp.OutputColumns.Add(outputColumn);
                            _logger.LogDebug("Added output {OutputName} to procedure {ProcedureName}", outputColumn.Name, currentSp.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error processing output column for procedure {ProcedureName}. Continuing with next column", currentSp.Name);
                            // Continue with next output
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing stored procedure row for {Procedure}", currentSpName);
                }
            }



            _logger.LogDebug("Extracted {ProcedureCount} stored procedures with {ParameterCount} total parameters",
                spCount, paramCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting stored procedures");
            throw;
        }
    }

    private async Task ExtractUserFunctions(SqlConnection connection, DBConnectionDatabaseSchemaDto response)
    {
        try
        {
            _logger.LogDebug("Starting user function extraction");
            var functionQuery = @"
                SELECT 
                    SCHEMA_NAME(SCHEMA_ID) as SPECIFIC_SCHEMA,
                    o.name as SPECIFIC_NAME,
                    p.name as PARAMETER_NAME,
                    TYPE_NAME(p.user_type_id) as DATA_TYPE,
                    CASE 
                        WHEN p.is_output = 1 THEN 'OUT'
                        ELSE 'IN'
                    END as PARAMETER_MODE
                FROM sys.objects o
                LEFT JOIN sys.parameters p ON o.object_id = p.object_id
                WHERE o.type IN ('FN', 'IF', 'TF')  -- FN: Scalar Function, IF: Inline Table Function, TF: Table Function
                AND SCHEMA_NAME(SCHEMA_ID) != 'sys'
                ORDER BY SCHEMA_NAME(SCHEMA_ID), o.name, p.parameter_id";

            using var command = new SqlCommand(functionQuery, connection);
            using var reader = await command.ExecuteReaderAsync();

            DbConnectionUserFunctionDescriptionDto currentFunc = null;
            string currentFuncName = null;
            string currentSchema = null;
            int funcCount = 0;
            int paramCount = 0;

            while (await reader.ReadAsync())
            {
                try
                {
                    var schema = reader.GetString(0);
                    var funcName = reader.GetString(1);

                    if (currentFuncName != funcName || currentSchema != schema)
                    {
                        currentFunc = new DbConnectionUserFunctionDescriptionDto
                        {
                            Name = funcName,
                            Schema = schema
                        };
                        response.UserFunctions.Add(currentFunc);
                        currentFuncName = funcName;
                        currentSchema = schema;
                        funcCount++;
                        _logger.LogTrace("Processing user function {Schema}.{Function}", schema, funcName);
                    }

                    if (!reader.IsDBNull(2)) // Skip return value parameter
                    {
                        currentFunc.Parameters.Add(new DBConnectionParameterDescriptionDto
                        {
                            Name = reader.GetString(2),
                            DataType = reader.GetString(3),
                            Mode = reader.GetString(4)
                        });
                        paramCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing user function row for {Function}", currentFuncName);
                }
            }

            _logger.LogDebug("Extracted {FunctionCount} user functions with {ParameterCount} total parameters",
                funcCount, paramCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting user functions");
            throw;
        }
    }

    private string SqlTypeToCsSqlParameterType(string sqlType)
    {
        string csType = "";
        if (sqlType.Contains('('))
        {
            sqlType = sqlType.Substring(0, sqlType.IndexOf('('));
        }
        if (!Enum.TryParse(sqlType, out MSSQLNativeType typeCode))
        {
            throw new Exception($"sql type {sqlType} inconnu");
        }
        switch (typeCode)
        {
            case MSSQLNativeType.filestream:
            case MSSQLNativeType.binary:
            case MSSQLNativeType.varbinary:
                csType = "System.Data.SqlDbType.VarBinary";
                break;
            case MSSQLNativeType.image:
                csType = "System.Data.SqlDbType.Binary";
                break;
            case MSSQLNativeType.timestamp:
            case MSSQLNativeType.rowversion:
                csType = "System.Data.SqlDbType.Timestamp";
                break;
            case MSSQLNativeType.tinyint:
                csType = "System.Data.SqlDbType.TinyInt";
                break;
            case MSSQLNativeType.varchar:
                csType = "System.Data.SqlDbType.VarChar";
                break;
            case MSSQLNativeType.nvarchar:
                csType = "System.Data.SqlDbType.NVarChar";
                break;
            case MSSQLNativeType.nchar:
                csType = "System.Data.SqlDbType.NChar";
                break;
            case MSSQLNativeType.text:
                csType = "System.Data.SqlDbType.Text";
                break;
            case MSSQLNativeType.ntext:
                csType = "System.Data.SqlDbType.NText";
                break;
            case MSSQLNativeType.xml:
                csType = "System.Data.SqlDbType.Xml";
                break;
            case MSSQLNativeType.@char:
                csType = "System.Data.SqlDbType.Char";
                break;
            case MSSQLNativeType.bigint:
                csType = "System.Data.SqlDbType.BigInt";
                break;
            case MSSQLNativeType.bit:
                csType = "System.Data.SqlDbType.Bit";
                break;
            case MSSQLNativeType.smalldatetime:
            case MSSQLNativeType.datetime:
                csType = "System.Data.SqlDbType.DateTime";
                break;
            case MSSQLNativeType.date:
                csType = "System.Data.SqlDbType.Date";
                break;
            case MSSQLNativeType.datetime2:
                csType = "System.Data.SqlDbType.DateTime2";
                break;
            case MSSQLNativeType.datetimeoffset:
                csType = "System.Data.SqlDbType.DateTimeOffset";
                break;
            case MSSQLNativeType.@decimal:
            case MSSQLNativeType.money:
            case MSSQLNativeType.numeric:
            case MSSQLNativeType.smallmoney:
                csType = "System.Data.SqlDbType.Decimal";
                break;
            case MSSQLNativeType.@float:
                csType = "System.Data.SqlDbType.Float";
                break;
            case MSSQLNativeType.@int:
                csType = "System.Data.SqlDbType.Int";
                break;
            case MSSQLNativeType.real:
                csType = "System.Data.SqlDbType.Real";
                break;
            case MSSQLNativeType.smallint:
                csType = "System.Data.SqlDbType.SmallInt";
                break;
            case MSSQLNativeType.uniqueidentifier:
                csType = "System.Data.SqlDbType.UniqueIdentifier";
                break;
            case MSSQLNativeType.sql_variant:
                csType = "System.Data.SqlDbType.Variant";
                break;
            case MSSQLNativeType.time:
                csType = "System.Data.SqlDbType.Time";
                break;
            default:
                throw new Exception("none equal type");
        }
        return csType;
    }
    private string SqlTypeToCsType(string sqlType, bool isNullable, int length = 1)
    {
        string csType = "";
        if (sqlType.Contains('('))
        {
            sqlType = sqlType.Substring(0, sqlType.IndexOf('('));
        }
        if (!Enum.TryParse(sqlType, out MSSQLNativeType typeCode))
        {
            throw new Exception($"sql type {sqlType} inconnu");
        }
        switch (typeCode)
        {
            case MSSQLNativeType.varbinary:
            case MSSQLNativeType.binary:
            case MSSQLNativeType.filestream:
            case MSSQLNativeType.image:
            case MSSQLNativeType.rowversion:
            case MSSQLNativeType.timestamp://?
                csType = "byte[]";
                break;
            case MSSQLNativeType.tinyint:
                csType = "byte";
                break;
            case MSSQLNativeType.varchar:
            case MSSQLNativeType.nvarchar:
            case MSSQLNativeType.nchar:
            case MSSQLNativeType.text:
            case MSSQLNativeType.ntext:
            case MSSQLNativeType.xml:
                csType = "string";
                break;
            case MSSQLNativeType.@char:
                if (length > 1)
                    csType = "string";
                else
                    csType = "char";
                break;
            case MSSQLNativeType.bigint:
                csType = "long";
                break;
            case MSSQLNativeType.bit:
                csType = "bool";
                break;
            case MSSQLNativeType.smalldatetime:
            case MSSQLNativeType.datetime:
            case MSSQLNativeType.date:
            case MSSQLNativeType.datetime2:
                csType = "DateTime";
                break;
            case MSSQLNativeType.datetimeoffset:
                csType = "DateTimeOffset";
                break;
            case MSSQLNativeType.@decimal:
            case MSSQLNativeType.money:
            case MSSQLNativeType.numeric:
            case MSSQLNativeType.smallmoney:
                csType = "decimal";
                break;
            case MSSQLNativeType.@float:
                csType = "double";
                break;
            case MSSQLNativeType.@int:
                csType = "int";
                break;
            case MSSQLNativeType.real:
                csType = "Single";
                break;
            case MSSQLNativeType.smallint:
                csType = "short";
                break;
            case MSSQLNativeType.uniqueidentifier:
                csType = "Guid";
                break;
            case MSSQLNativeType.sql_variant:
                csType = "object";
                break;
            case MSSQLNativeType.time:
                csType = "TimeSpan";
                break;
            default:
                throw new Exception("none equal type");
        }

        if (isNullable)
            csType += "?";

        return csType;
    }
}