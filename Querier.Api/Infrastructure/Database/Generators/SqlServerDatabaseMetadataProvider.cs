using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Querier.Api.Domain.Common.Enums;
using Querier.Api.Domain.Entities.QDBConnection;
using Querier.Api.Domain.Services;
using Querier.Api.Infrastructure.Database.Templates;

namespace Querier.Api.Infrastructure.Database.Generators;

public class SqlServerDatabaseMetadataProvider : DatabaseMetadataProviderBase, IDatabaseMetadataProvider
{
    private readonly ILogger<SqlServerDatabaseMetadataProvider> _logger;
    public SqlServerDatabaseMetadataProvider(ILogger<SqlServerDatabaseMetadataProvider> logger) : base()
    {
        _logger = logger;
    }
    
    public List<StoredProcedureMetadata> ExtractStoredProcedureMetadata(string connectionString)
    {
        List<StoredProcedureMetadata> result = [];
        
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
                // AI Analysis
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
                SqlParameterType = SqlTypeToCsSqlParameterType((string)parameterReader["Type"]),
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
                SqlParameterType = SqlTypeToCsSqlParameterType((string)row["system_type_name"]),
                CSType = SqlTypeToCsType((string)row["system_type_name"], false)
            };
            result.Add(outputSet);
            _logger.LogDebug("Added output {OutputName} to procedure {ProcedureName}", outputSet.Name, procedureName);
        }
        
        return result;
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