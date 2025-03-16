using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Npgsql;
using Microsoft.Extensions.Logging;
using Querier.Api.Infrastructure.Database.Templates;

namespace Querier.Api.Infrastructure.Database.Generators
{
    /// <summary>
    /// Adaptation PostgreSQL du même principe que SqlServerDatabaseMetadataProvider
    /// </summary>
    public class PostgreSqlDatabaseMetadataProvider(ILogger logger)
        : DatabaseMetadataProviderBase, IDatabaseMetadataProvider
    {
        /// <summary>
        /// Liste les procédures stockées (PostgreSQL 11+ gère CREATE PROCEDURE).
        /// 
        /// ATTENTION : Par défaut, on liste toutes les procédures du schema courant.
        /// Si besoin, filtrer sur routine_schema = 'myschema'.
        /// </summary>
        public List<StoredProcedureMetadata> ExtractStoredProcedureMetadata(string connectionString)
        {
            List<StoredProcedureMetadata> result = new();

            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            using var listProceduresCommand = connection.CreateCommand();
            listProceduresCommand.CommandText = @"
                SELECT routine_schema, routine_name
                  FROM information_schema.routines
                 WHERE routine_type = 'PROCEDURE'
                   AND specific_schema NOT IN ('pg_catalog', 'information_schema')
                 ORDER BY routine_name
            ";

            using (var procedureReader = listProceduresCommand.ExecuteReader())
            {
                while (procedureReader.Read())
                {
                    string schemaName = procedureReader.GetString(0);
                    string procedureName = procedureReader.GetString(1);

                    logger.LogDebug("Processing stored procedure: {ProcedureName}", procedureName);

                    var procedure = new StoredProcedureMetadata
                    {
                        Schema = schemaName,
                        Name = procedureName,
                        CSName = NormalizeCsString(procedureName),
                        Parameters = new List<TemplateProperty>(),
                        OutputSet = new List<TemplateProperty>()
                    };

                    result.Add(procedure);
                }
            }

            List<string> procedureToRemove = [];
            foreach (var procedure in result)
            {
                try
                {
                    procedure.Parameters = GetProcedureParametersMetadata(connection, procedure.Schema, procedure.Name);
                }
                catch (Exception ex)
                {
                    procedureToRemove.Add(procedure.Name);
                    logger.LogError(ex, "Unable to get parameters metadata for {ProcedureName}", procedure.Name);
                }

                try
                {
                    procedure.OutputSet = GetProcedureOutputMetadata(connection, procedure.Schema, procedure.Name,
                        procedure.Parameters.Count);
                }
                catch (Exception ex)
                {
                    procedureToRemove.Add(procedure.Name);
                    logger.LogError(ex, "Unable to get output set metadata for {ProcedureName}", procedure.Name);
                }
            }
            
            foreach (var index in procedureToRemove.Select(procedureName => result.FindIndex(p => p.Name == procedureName)))
            {
                result.RemoveAt(index);
            }
            
            return result;
        }

        /// <summary>
        /// Récupère la liste des paramètres d’une procédure PostgreSQL depuis information_schema.parameters
        /// </summary>
        private List<TemplateProperty> GetProcedureParametersMetadata(NpgsqlConnection connection, string schemaName, string procedureName)
        {
            List<TemplateProperty> result = new();

            // Paramètres : information_schema.parameters 
            // key :  parameter_name, data_type, parameter_mode (IN, OUT, INOUT)
            using var listParametersCmd = connection.CreateCommand();
            listParametersCmd.CommandText = @"
                SELECT
                    p.parameter_name,
                    p.data_type,
                    p.parameter_mode,
                    p.ordinal_position,
                    r.routine_name   -- récupéré de routines
                FROM information_schema.parameters p
                         JOIN information_schema.routines r
                              ON p.specific_schema = r.specific_schema
                                  AND p.specific_name   = r.specific_name
                WHERE r.routine_schema = @schemaName
                  AND r.routine_name = @procedureName
                ORDER BY p.ordinal_position;
            ";
            listParametersCmd.Parameters.AddWithValue("@schemaName", schemaName);
            listParametersCmd.Parameters.AddWithValue("@procedureName", procedureName);

            using var parameterReader = listParametersCmd.ExecuteReader();
            while (parameterReader.Read())
            {
                string paramName = parameterReader.IsDBNull(0) ? "" : parameterReader.GetString(0);
                string dataType = parameterReader.IsDBNull(1) ? "" : parameterReader.GetString(1);
                string paramMode = parameterReader.IsDBNull(2) ? "IN" : parameterReader.GetString(2);

                var parameter = new TemplateProperty
                {
                    Name = paramName,
                    CSName = NormalizeCsString(paramName),
                    IsKey = false,
                    IsForeignKey = false,
                    IsRequired = true,
                    IsAutoGenerated = false,
                    CSType = PgTypeToCsType(dataType, paramMode.Equals("INOUT", StringComparison.OrdinalIgnoreCase)),
                };

                logger.LogDebug("Added parameter {ParameterName} ({DataType}) to procedure {ProcedureName}", paramName, dataType, procedureName);
                result.Add(parameter);
            }

            return result;
        }

        /// <summary>
        /// Récupère le schéma de la sortie via exécution en mode schéma seulement.
        /// PostgreSQL n'a pas sp_describe_first_result_set, on fait un CALL ... 
        /// </summary>
        private List<TemplateProperty> GetProcedureOutputMetadata(
            NpgsqlConnection connection, 
            string schemaName, 
            string procedureName,
            int parameterCount
        )
        {
            List<TemplateProperty> result = new();

            // On exécute la procédure en mode "SchemaOnly"
            // Si la procédure n'accepte pas d'arguments, paramCount=0 => pas de placeholders.
            var paramPlaceholder = string.Join(", ", new int[parameterCount].Select(_ => "NULL"));
            string callSql = $"CALL \"{schemaName}\".\"{procedureName}\"({paramPlaceholder});";

            // Pour éviter de polluer la BDD si la procédure fait des INSERT/UPDATE, 
            // on peut enclencher une transaction + rollback
            using var transaction = connection.BeginTransaction();
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = callSql;

            using var reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly);
            var schemaTable = reader.GetSchemaTable();
            if (schemaTable == null)
            {
                logger.LogWarning("No schema table found for procedure {ProcedureName}", procedureName);
                transaction.Rollback();
                return result;
            }

            foreach (DataRow row in schemaTable.Rows)
            {
                string colName = row["ColumnName"] as string ?? "";
                // row["DataType"] est souvent un System.Type en .NET
                // row["DataTypeName"] peut exister pour un driver Npgsql plus récent
                string colTypeName = row["DataTypeName"] as string ?? ""; 

                var outputProperty = new TemplateProperty
                {
                    Name = colName,
                    CSName = NormalizeCsString(colName),
                    IsKey = false,
                    IsForeignKey = false,
                    IsRequired = true,
                    IsAutoGenerated = false,
                    CSType = PgTypeToCsType(colTypeName, true),
                };

                logger.LogDebug("Added output {OutputName} to procedure {ProcedureName}", outputProperty.Name, procedureName);
                result.Add(outputProperty);
            }

            // Annule les modifications éventuelles
            transaction.Rollback();

            return result;
        }

        /// <summary>
        /// Convertit un type PostgreSQL (ex: 'integer', 'text', 'bytea', etc.) en un type .NET (System.Data.DbType ou string).
        /// Ici on imite la logique SQLServer, 
        /// mais la nomenclature PG est différente (e.g. 'integer', 'bigint', 'boolean', 'timestamp without time zone', etc.).
        /// </summary>
        private string PgTypeToCsSqlParameterType(string pgType)
        {
            // Simplifié: on gère les cas de base
            // Selon les besoins, on peut affiner (e.g. numeric(x,y) => Decimal)
            pgType = pgType.ToLowerInvariant();
            return pgType switch
            {
                "integer"        => "System.Data.DbType.Int32",
                "bigint"         => "System.Data.DbType.Int64",
                "smallint"       => "System.Data.DbType.Int16",
                "boolean"        => "System.Data.DbType.Boolean",
                "real"           => "System.Data.DbType.Single",
                "double precision" => "System.Data.DbType.Double",
                "numeric" or "money" => "System.Data.DbType.Decimal",
                "character varying" or "text" or "varchar" or "char"  or "character"
                                     => "System.Data.DbType.String",
                "timestamp" or "timestamp without time zone" 
                                     => "System.Data.DbType.DateTime",
                "timestamp with time zone" 
                                     => "System.Data.DbType.DateTimeOffset",
                "date"           => "System.Data.DbType.Date",
                "time" or "time without time zone" 
                                     => "System.Data.DbType.Time",
                "time with time zone" 
                                     => "System.Data.DbType.Object", // Pas de type direct
                "bytea"          => "System.Data.DbType.Binary",
                "uuid"           => "System.Data.DbType.Guid",
                _                => "System.Data.DbType.Object"
            };
        }

        /// <summary>
        /// Convertit un type PostgreSQL en type C#.
        /// </summary>
        private string PgTypeToCsType(string pgType, bool isNullable)
        {
            // On peut ajouter un paramètre length si besoin
            pgType = pgType.ToLowerInvariant();
            string csType = pgType switch
            {
                "integer"                 => "int",
                "bigint"                  => "long",
                "smallint"                => "short",
                "boolean"                 => "bool",
                "real"                    => "float",
                "double precision"        => "double",
                "numeric" or "money"      => "decimal",
                "character varying" 
                or "character"
                or "text" 
                or "varchar" 
                or "char"                 => "string",
                "timestamp" 
                or "timestamp without time zone" => "DateTime",
                "timestamp with time zone"=> "DateTimeOffset",
                "date"                    => "DateTime",
                "time" 
                or "time without time zone"      => "TimeSpan",
                "time with time zone"     => "string", // ou DateTimeOffset partiel
                "bytea"                   => "byte[]",
                "uuid"                    => "Guid",
                _                         => "object"
            };

            if (isNullable && csType != "string" && csType != "object" && csType != "byte[]")
            {
                csType += "?";
            }
            return csType;
        }
    }
}
