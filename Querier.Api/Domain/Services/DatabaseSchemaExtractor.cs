using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs.Responses.DBConnection;
using Querier.Api.Domain.Common.Enums;

namespace Querier.Api.Domain.Services
{
    public class DatabaseSchemaExtractor
    {
        private readonly ILogger<DatabaseSchemaExtractor> _logger;

        public DatabaseSchemaExtractor(ILogger<DatabaseSchemaExtractor> logger)
        {
            _logger = logger;
        }

        public async Task<DatabaseSchemaResponse> ExtractSchema(QDBConnectionType connectionType, string connectionString)
        {
            var response = new DatabaseSchemaResponse();

            try
            {
                switch (connectionType)
                {
                    case QDBConnectionType.SqlServer:
                        await GetSqlServerSchema(connectionString, response);
                        break;
                    case QDBConnectionType.MySQL:
                        await GetMySqlSchema(connectionString, response);
                        break;
                    case QDBConnectionType.PgSQL:
                        await GetPgSqlSchema(connectionString, response);
                        break;
                    default:
                        throw new NotSupportedException($"Database type {connectionType} not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database schema for connection string {ConnectionString}", connectionString);
                throw;
            }

            return response;
        }

        private async Task GetSqlServerSchema(string connectionString, DatabaseSchemaResponse response)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await ExtractTables(connection, response);
            await ExtractViews(connection, response);
            await ExtractStoredProcedures(connection, response);
            await ExtractUserFunctions(connection, response);
        }

        private async Task ExtractTables(SqlConnection connection, DatabaseSchemaResponse response)
        {
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

            TableDescription currentTable = null;
            string currentTableName = null;
            string currentSchema = null;

            while (await reader.ReadAsync())
            {
                var schema = reader.GetString(0);
                var tableName = reader.GetString(1);

                if (currentTableName != tableName || currentSchema != schema)
                {
                    currentTable = new TableDescription
                    {
                        Name = tableName,
                        Schema = schema
                    };
                    response.Tables.Add(currentTable);
                    currentTableName = tableName;
                    currentSchema = schema;
                }

                currentTable.Columns.Add(new ColumnDescription
                {
                    Name = reader.GetString(2),
                    DataType = reader.GetString(3),
                    IsNullable = reader.GetString(4) == "YES",
                    IsPrimaryKey = reader.GetInt32(5) == 1,
                    IsForeignKey = reader.GetInt32(6) == 1,
                    ForeignKeyTable = !reader.IsDBNull(7) ? reader.GetString(7) : null,
                    ForeignKeyColumn = !reader.IsDBNull(8) ? reader.GetString(8) : null
                });
            }
        }

        private async Task ExtractViews(SqlConnection connection, DatabaseSchemaResponse response)
        {
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

            ViewDescription currentView = null;
            string currentViewName = null;
            string currentSchema = null;

            while (await reader.ReadAsync())
            {
                var schema = reader.GetString(0);
                var viewName = reader.GetString(1);

                if (currentViewName != viewName || currentSchema != schema)
                {
                    currentView = new ViewDescription
                    {
                        Name = viewName,
                        Schema = schema
                    };
                    response.Views.Add(currentView);
                    currentViewName = viewName;
                    currentSchema = schema;
                }

                currentView.Columns.Add(new ColumnDescription
                {
                    Name = reader.GetString(2),
                    DataType = reader.GetString(3),
                    IsNullable = reader.GetString(4) == "YES"
                });
            }
        }

        private async Task ExtractStoredProcedures(SqlConnection connection, DatabaseSchemaResponse response)
        {
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

            StoredProcedureDescription currentSp = null;
            string currentSpName = null;
            string currentSchema = null;

            while (await reader.ReadAsync())
            {
                var schema = reader.GetString(0);
                var spName = reader.GetString(1);

                if (currentSpName != spName || currentSchema != schema)
                {
                    currentSp = new StoredProcedureDescription
                    {
                        Name = spName,
                        Schema = schema
                    };
                    response.StoredProcedures.Add(currentSp);
                    currentSpName = spName;
                    currentSchema = schema;
                }

                if (!reader.IsDBNull(2)) // Skip return value parameter
                {
                    currentSp.Parameters.Add(new ParameterDescription
                    {
                        Name = reader.GetString(2),
                        DataType = reader.GetString(3),
                        Mode = reader.GetString(4)
                    });
                }
            }
        }

        private async Task ExtractUserFunctions(SqlConnection connection, DatabaseSchemaResponse response)
        {
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

            UserFunctionDescription currentFunc = null;
            string currentFuncName = null;
            string currentSchema = null;

            while (await reader.ReadAsync())
            {
                var schema = reader.GetString(0);
                var funcName = reader.GetString(1);

                if (currentFuncName != funcName || currentSchema != schema)
                {
                    currentFunc = new UserFunctionDescription
                    {
                        Name = funcName,
                        Schema = schema
                    };
                    response.UserFunctions.Add(currentFunc);
                    currentFuncName = funcName;
                    currentSchema = schema;
                }

                if (!reader.IsDBNull(2)) // Skip return value parameter
                {
                    currentFunc.Parameters.Add(new ParameterDescription
                    {
                        Name = reader.GetString(2),
                        DataType = reader.GetString(3),
                        Mode = reader.GetString(4)
                    });
                }
            }
        }

        private Task GetMySqlSchema(string connectionString, DatabaseSchemaResponse response)
        {
            // TODO: Implémenter la logique spécifique à MySQL
            throw new NotImplementedException("MySQL schema extraction not yet implemented");
        }

        private Task GetPgSqlSchema(string connectionString, DatabaseSchemaResponse response)
        {
            // TODO: Implémenter la logique spécifique à PostgreSQL
            throw new NotImplementedException("PostgreSQL schema extraction not yet implemented");
        }
    }
} 