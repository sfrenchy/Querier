using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Querier.Api.Domain.Entities.DBConnection;
using Querier.Api.Domain.Entities.QDBConnection;
using Querier.Api.Infrastructure.Database.Templates;

namespace Querier.Api.Infrastructure.Database.Generators;

public class MySqlDatabaseProvider(ILogger logger) : IDatabaseMetadataProvider
{
    public DbConnection CreateConnection(string connectionString) => new MySqlConnection(connectionString);

    public List<StoredProcedure> GetStoredProcedures(DbConnection connection)
    {
        logger.LogInformation("Starting to convert MySQL database stored procedures to C# objects");
        List<StoredProcedure> procedures = new List<StoredProcedure>();

        try
        {
            // On suppose que la connexion est déjà ouverte
            logger.LogDebug("Database connection opened successfully");

            using var cmd = connection.CreateCommand();

            // Récupère le schéma et le nom des procédures 
            // uniquement pour le schéma courant (DATABASE())
            cmd.CommandText = @"
            SELECT ROUTINE_SCHEMA, ROUTINE_NAME
            FROM INFORMATION_SCHEMA.ROUTINES
            WHERE ROUTINE_TYPE = 'PROCEDURE'
              AND ROUTINE_SCHEMA = DATABASE()
            ORDER BY ROUTINE_NAME";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string schemaName = reader.GetString(0);
                string procedureName = reader.GetString(1);

                logger.LogDebug("Processing stored procedure: {ProcedureName}", procedureName);

                var procedure = new StoredProcedure
                {
                    Schema = schemaName,
                    Name = procedureName,
                    Parameters = new List<ProcedureParameter>(),
                    OutputSet = new List<ProcedureOutput>()
                };

                // Ajout dans la liste
                procedures.Add(procedure);
                logger.LogInformation("Successfully processed stored procedure: {ProcedureName}", procedureName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred while converting database to C#");
            throw;
        }

        logger.LogInformation("Completed database conversion. Found {Count} procedures", procedures.Count);
        return procedures;
    }

    public void LoadProcedureParameters(DbConnection connection, StoredProcedure procedure)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
        SELECT
            PARAMETER_NAME AS Parameter,
            DATA_TYPE AS Type,
            CHARACTER_MAXIMUM_LENGTH AS Length,
            NUMERIC_PRECISION AS `ParamPrecision`,
            NUMERIC_SCALE AS `ParamScale`,
            ORDINAL_POSITION AS `Order`,
            CASE WHEN PARAMETER_MODE = 'OUT' THEN 1 ELSE 0 END AS is_Output,
            1 AS is_nullable
        FROM INFORMATION_SCHEMA.PARAMETERS
        WHERE SPECIFIC_SCHEMA = DATABASE()
        AND SPECIFIC_NAME = @procName
        ORDER BY ORDINAL_POSITION";

        cmd.Parameters.Add(new MySqlParameter("@procName", procedure.Name));

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var parameter = new ProcedureParameter
            {
                Name = reader["Parameter"] as string ?? string.Empty,
                SQLType = reader["Type"] as string ?? string.Empty,
                Length = reader["Length"] != DBNull.Value ? Convert.ToInt32(reader["Length"]) : 0,
                Precision = reader["ParamPrecision"] != DBNull.Value ? Convert.ToInt32(reader["ParamPrecision"]) : 0,
                Order = Convert.ToInt32(reader["Order"]),
                IsOutput = Convert.ToInt32(reader["is_Output"]) == 1,
                IsNullable = Convert.ToInt32(reader["is_nullable"]) == 1
            };

            procedure.Parameters.Add(parameter);
            logger.LogDebug("Added parameter {ParameterName} to procedure {ProcedureName}", parameter.Name, procedure.Name);
        }
    }

    public void LoadProcedureOutputs(DbConnection connection, StoredProcedure procedure)
    {
        try
        {
            using var cmd = connection.CreateCommand();
        
            // Construire l'appel avec des paramètres fictifs (NULL)
            string safeName = procedure.Name.Replace("`", "``"); 
            var paramPlaceholder = string.Join(", ", procedure.Parameters.Select(p => "NULL"));
            cmd.CommandText = $"CALL `{safeName}`({paramPlaceholder});";

            using var reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly);
            var schemaTable = reader.GetSchemaTable();
            if (schemaTable == null) return;

            int order = 0;
            foreach (DataRow row in schemaTable.Rows)
            {
                var outputSet = new ProcedureOutput
                {
                    Name = row["ColumnName"] as string ?? string.Empty,
                    Order = order++,
                    IsNullable = row["AllowDBNull"] != DBNull.Value && (bool)row["AllowDBNull"],
                    SQLType = row["DataTypeName"] as string ?? string.Empty
                };

                procedure.OutputSet.Add(outputSet);
                logger.LogDebug("Added output {OutputName} to procedure {ProcedureName}", outputSet.Name, procedure.Name);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting output set for procedure {ProcedureName}", procedure.Name);
        }
    }

    public List<StoredProcedureMetadata> ExtractStoredProcedureMetadata(string connectionString)
    {
        throw new NotImplementedException();
    }
}