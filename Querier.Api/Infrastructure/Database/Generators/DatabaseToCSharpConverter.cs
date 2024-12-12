using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Querier.Api.Domain.Entities.QDBConnection;

namespace Querier.Api.Infrastructure.Database.Generators
{
    public static class DatabaseToCSharpConverter
    {
        public static List<StoredProcedure> ToProcedureList(string connectionString)
        {
            List<StoredProcedure> procedures = new List<StoredProcedure>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand getProcedures = new SqlCommand(@"
                    SELECT * 
                      FROM INFORMATION_SCHEMA.ROUTINES
                     WHERE ROUTINE_TYPE = 'PROCEDURE' 
                       AND LEFT(ROUTINE_NAME, 3) NOT IN ('sp_', 'xp_', 'ms_')
                     ORDER BY ROUTINE_NAME", connection);
                SqlDataReader procReader = getProcedures.ExecuteReader();
                try
                {
                    while (procReader.Read())
                    {
                        StoredProcedure procedure = new StoredProcedure();
                        procedure.Name = (string)procReader["ROUTINE_NAME"];
                        procedure.Parameters = new List<ProcedureParameter>();
                        procedure.OutputSet = new List<ProcedureOutput>();
                        try
                        {
                            SqlCommand getProcedureParams = new SqlCommand(@"
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
                            SqlDataReader procParamsReader = getProcedureParams.ExecuteReader();
                            try
                            {
                                while (procParamsReader.Read())
                                {
                                    ProcedureParameter parameter = new ProcedureParameter
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
                                }
                            }
                            finally
                            {
                                procParamsReader.Close();
                            }

                            SqlCommand getProcedureOutput = new SqlCommand("sp_describe_first_result_set", connection);
                            getProcedureOutput.CommandType = CommandType.StoredProcedure;
                            getProcedureOutput.Parameters.AddWithValue("@tsql", "dbo." + procedure.Name);
                            SqlDataAdapter da = new SqlDataAdapter(getProcedureOutput);
                            DataTable dt = new DataTable();
                            da.Fill(dt);

                            SqlDataReader procOutputReader = getProcedureOutput.ExecuteReader();
                            try
                            {
                                foreach (DataRow row in dt.Rows)
                                {
                                    ProcedureOutput outputSet = new ProcedureOutput
                                    {
                                        Name = (string)row["name"],
                                        Order = (int)row["column_ordinal"],
                                        IsNullable = Convert.ToInt32(row["is_nullable"]) == 1,
                                        SQLType = (string)row["system_type_name"]
                                    };

                                    procedure.OutputSet.Add(outputSet);
                                }
                            }
                            finally
                            {
                                procOutputReader.Close();
                            }
                            procedures.Add(procedure);
                        }
                        catch (Exception e)
                        {

                        }
                    }
                }
                finally
                {
                    // Always call Close when done reading.
                    procReader.Close();
                }
            }
            return procedures;
        }
    }
}
