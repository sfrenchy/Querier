using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Querier.Api.Domain.Entities.QDBConnection;
using Microsoft.Extensions.Logging;

namespace Querier.Api.Infrastructure.Database.Generators
{
    public class DatabaseToCSharpConverter
    {
        private readonly ILogger<DatabaseToCSharpConverter> _logger;

        public DatabaseToCSharpConverter(ILogger<DatabaseToCSharpConverter> logger)
        {
            _logger = logger;
        }

        public List<StoredProcedure> ToProcedureList(string connectionString)
        {
            List<StoredProcedure> procedures = new List<StoredProcedure>();
            _logger.LogInformation("Starting to convert database stored procedures to C# objects");

            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();
                _logger.LogDebug("Database connection opened successfully");

                SqlCommand getProcedures = new SqlCommand(@"
                    SELECT * 
                      FROM INFORMATION_SCHEMA.ROUTINES
                     WHERE ROUTINE_TYPE = 'PROCEDURE' 
                       AND LEFT(ROUTINE_NAME, 3) NOT IN ('sp_', 'xp_', 'ms_')
                     ORDER BY ROUTINE_NAME", connection);

                using SqlDataReader procReader = getProcedures.ExecuteReader();
                while (procReader.Read())
                {
                    string procedureName = (string)procReader["ROUTINE_NAME"];
                    _logger.LogDebug("Processing stored procedure: {ProcedureName}", procedureName);

                    StoredProcedure procedure = new StoredProcedure
                    {
                        Name = procedureName,
                        Parameters = new List<ProcedureParameter>(),
                        OutputSet = new List<ProcedureOutput>()
                    };

                    try
                    {
                        LoadProcedureParameters(connection, procedure);
                        LoadProcedureOutputs(connection, procedure);
                        procedures.Add(procedure);
                        _logger.LogInformation("Successfully processed stored procedure: {ProcedureName}", procedureName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing stored procedure {ProcedureName}. Continuing with next procedure", procedureName);
                        // Continue with next procedure
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error occurred while converting database to C#");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while converting database to C#");
                throw;
            }

            _logger.LogInformation("Completed database conversion. Found {Count} procedures", procedures.Count);
            return procedures;
        }

        private void LoadProcedureParameters(SqlConnection connection, StoredProcedure procedure)
        {
            using var getProcedureParams = new SqlCommand(@"
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
                  FROM sys.parameters where object_id = object_id('dbo." + procedure.Name + @"')
                 ORDER BY parameter_id", connection);

            using var procParamsReader = getProcedureParams.ExecuteReader();
            while (procParamsReader.Read())
            {
                try
                {
                    var parameter = new ProcedureParameter
                    {
                        Name = (string)procParamsReader["Parameter"],
                        SQLType = (string)procParamsReader["Type"],
                        Length = Convert.ToInt32(procParamsReader["Length"]),
                        Precision = Convert.ToInt32(procParamsReader["Precision"]),
                        Order = Convert.ToInt32(procParamsReader["Order"]),
                        IsOutput = Convert.ToInt32(procParamsReader["is_Output"]) == 1,
                        IsNullable = Convert.ToInt32(procParamsReader["is_nullable"]) == 1
                    };

                    procedure.Parameters.Add(parameter);
                    _logger.LogDebug("Added parameter {ParameterName} to procedure {ProcedureName}", parameter.Name, procedure.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing parameter for procedure {ProcedureName}. Continuing with next parameter", procedure.Name);
                    // Continue with next parameter
                }
            }
        }

        private void LoadProcedureOutputs(SqlConnection connection, StoredProcedure procedure)
        {
            try
            {
                using var getProcedureOutput = new SqlCommand("sp_describe_first_result_set", connection);
                getProcedureOutput.CommandType = CommandType.StoredProcedure;
                getProcedureOutput.Parameters.AddWithValue("@tsql", $"EXEC [dbo].[{procedure.Name}]");

                using var da = new SqlDataAdapter(getProcedureOutput);
                var dt = new DataTable();
                da.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        var outputSet = new ProcedureOutput
                        {
                            Name = (string)row["name"],
                            Order = (int)row["column_ordinal"],
                            IsNullable = Convert.ToInt32(row["is_nullable"]) == 1,
                            SQLType = (string)row["system_type_name"]
                        };

                        procedure.OutputSet.Add(outputSet);
                        _logger.LogDebug("Added output {OutputName} to procedure {ProcedureName}", outputSet.Name, procedure.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing output column for procedure {ProcedureName}. Continuing with next column", procedure.Name);
                        // Continue with next output
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting output set for procedure {ProcedureName}", procedure.Name);
                // Continue without output set
            }
        }
    }
}
