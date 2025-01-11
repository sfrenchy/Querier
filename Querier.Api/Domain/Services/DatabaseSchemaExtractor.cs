using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Domain.Common.Enums;

namespace Querier.Api.Domain.Services
{
    public class DatabaseSchemaExtractor
    {
        private readonly ILogger<DatabaseSchemaExtractor> _logger;

        public DatabaseSchemaExtractor(ILogger<DatabaseSchemaExtractor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DBConnectionDatabaseSchemaDto> ExtractSchema(DbConnectionType connectionType, string connectionString)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(connectionString);
                _logger.LogInformation("Starting schema extraction for database type: {Type}", connectionType);

                var response = new DBConnectionDatabaseSchemaDto();

                switch (connectionType)
                {
                    case DbConnectionType.SqlServer:
                        _logger.LogDebug("Extracting SQL Server schema");
                        await GetSqlServerSchema(connectionString, response);
                        break;
                    case DbConnectionType.MySql:
                        _logger.LogDebug("Extracting MySQL schema");
                        await GetMySqlSchema(connectionString, response);
                        break;
                    case DbConnectionType.PgSql:
                        _logger.LogDebug("Extracting PostgreSQL schema");
                        await GetPgSqlSchema(connectionString, response);
                        break;
                    default:
                        var message = $"Database type {connectionType} not supported";
                        _logger.LogError(message);
                        throw new NotSupportedException(message);
                }

                _logger.LogInformation("Successfully extracted schema. Found {TableCount} tables, {ViewCount} views, {ProcCount} stored procedures, and {FuncCount} functions",
                    response.Tables.Count, response.Views.Count, response.StoredProcedures.Count, response.UserFunctions.Count);
                return response;
            }
            catch (ArgumentNullException)
            {
                throw;
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while extracting schema for {Type}", connectionType);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting schema for {Type}", connectionType);
                throw;
            }
        }

        private async Task GetSqlServerSchema(string connectionString, DBConnectionDatabaseSchemaDto response)
        {
            try
            {
                _logger.LogDebug("Opening SQL Server connection");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

                _logger.LogTrace("Extracting tables");
            await ExtractTables(connection, response);

                _logger.LogTrace("Extracting views");
            await ExtractViews(connection, response);

                _logger.LogTrace("Extracting stored procedures");
            await ExtractStoredProcedures(connection, response);

                _logger.LogTrace("Extracting user functions");
            await ExtractUserFunctions(connection, response);

                _logger.LogDebug("Successfully extracted SQL Server schema");
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

            using var command = new SqlCommand(spQuery, connection);
            using var reader = await command.ExecuteReaderAsync();

            DBConnectionStoredProcedureDescriptionDto currentSp = null;
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
                    currentSp = new DBConnectionStoredProcedureDescriptionDto
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

                if (!reader.IsDBNull(2)) // Skip return value parameter
                {
                    currentSp.Parameters.Add(new DBConnectionEndpointParameterDescriptionDto
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

            DBConnectionUserFunctionDescriptionDto currentFunc = null;
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
                    currentFunc = new DBConnectionUserFunctionDescriptionDto
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
                    currentFunc.Parameters.Add(new DBConnectionEndpointParameterDescriptionDto
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

        private Task GetMySqlSchema(string connectionString, DBConnectionDatabaseSchemaDto response)
        {
            _logger.LogWarning("MySQL schema extraction not yet implemented");
            throw new NotImplementedException("MySQL schema extraction not yet implemented");
        }

        private Task GetPgSqlSchema(string connectionString, DBConnectionDatabaseSchemaDto response)
        {
            _logger.LogWarning("PostgreSQL schema extraction not yet implemented");
            throw new NotImplementedException("PostgreSQL schema extraction not yet implemented");
        }
    }
} 