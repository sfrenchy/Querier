using System.Data;
using System;
using System.Data.Common;
using Querier.Api.Infrastructure.Database.Templates;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Npgsql;
using NpgsqlTypes;

namespace Querier.Api.Domain.Services
{
    public class ProcedureMetadataExtractorPgSql : ProcedureMetadataExtractorBase
    {
        private readonly NpgsqlConnection _connection;
        public ProcedureMetadataExtractorPgSql(string connectionString, DatabaseModel dbModel)
            : base(dbModel)
        {
            ConnectionString = connectionString;
            _connection = new NpgsqlConnection(connectionString);
            _connection.Open();
            ExtractMetadata();
        }
        protected override string GetProcedureWithParametersQuery => @"
        SELECT
            n.nspname AS SchemaName,
            p.proname AS ProcedureName,
            unnest(proargnames) AS ParameterName,
            unnest(string_to_array(oidvectortypes(proargtypes), ',')) AS DataType,
            NULL AS Length, -- PostgreSQL ne stocke pas directement la longueur des paramètres
            NULL AS Precision, -- Même chose pour la précision des types numériques
            NULL AS Scale, -- PostgreSQL ne fournit pas cette info directement
            generate_series(1, array_length(proargnames, 1)) AS ParameterOrder,
            NULL AS Collation, -- Peut être récupéré dans pg_collation pour certains types
            0 AS IsOutput,
            1 AS IsNullable
        FROM pg_catalog.pg_namespace n
                 JOIN pg_catalog.pg_proc p ON pronamespace = n.oid
                 JOIN pg_type t ON p.prorettype = t.oid
        WHERE n.nspname = 'public'
        GROUP BY n.nspname, p.proname, proargtypes, proargnames, proargmodes;
        ";

        protected override DbConnection Connection => _connection;

        protected override void ExtractProcedureOutputMetadata()
        {
            List<TemplateProperty> result = [];
            int procedureIndex = 0;
            List<int> procedureToRemoveIndexes = [];

            foreach (var procedure in _procedureMetadata)
            {
                try
                {
                    // Récupération des paramètres de la procédure depuis pg_proc et pg_namespace
                    var parameters = new List<NpgsqlParameter>();

                    using (var getParamsCommand = new NpgsqlCommand(@"
                        SELECT 
                            p.proname AS ProcedureName,
                            unnest(p.proargnames) AS ParameterName,
                            unnest(string_to_array(oidvectortypes(p.proargtypes), ',')) AS DataType
                        FROM pg_catalog.pg_proc p
                        JOIN pg_catalog.pg_namespace n ON n.oid = p.pronamespace
                        WHERE n.nspname = @schema AND p.proname = @procedure", _connection))
                    {
                        getParamsCommand.Parameters.AddWithValue("@schema", procedure.Schema);
                        getParamsCommand.Parameters.AddWithValue("@procedure", procedure.Name);

                        using var paramReader = getParamsCommand.ExecuteReader();
                        while (paramReader.Read())
                        {
                            var paramName = paramReader["ParameterName"].ToString();
                            var paramType = paramReader["DataType"].ToString();

                            var npgsqlParam = new NpgsqlParameter($"@{paramName}", GetNpgsqlDbType(paramType)); 
                            parameters.Add(npgsqlParam);
                        }
                    }

                    // Construction de la requête CALL avec des placeholders
                    string paramPlaceholders = string.Join(", ", parameters.Select(p => p.ParameterName));
                    string callProcedureSql = $"CALL \"{procedure.Schema}\".\"{procedure.Name}\"({paramPlaceholders})";

                    using var getProcedureOutput = new NpgsqlCommand(callProcedureSql, _connection);
                    
                    // Ajout des paramètres à la commande
                    foreach (var param in parameters)
                    {
                        getProcedureOutput.Parameters.Add(param);
                    }

                    using var reader = getProcedureOutput.ExecuteReader(CommandBehavior.SchemaOnly);
                    var schemaTable = reader.GetSchemaTable();

                    if (schemaTable != null)
                    {
                        foreach (DataRow row in schemaTable.Rows)
                        {
                            var outputSet = new TemplateProperty()
                            {
                                Name = (string)row["ColumnName"],
                                CSName = NormalizeCsString((string)row["ColumnName"]),
                                IsKey = false,
                                IsForeignKey = false,
                                IsRequired = (bool)row["AllowDBNull"] == false,
                                IsAutoGenerated = false,
                                CSType = row["DataType"].ToString()
                            };
                            procedure.OutputSet.Add(outputSet);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!TryAIOutputMetadataExtraction(procedure, result))
                    {
                        procedureToRemoveIndexes.Add(procedureIndex);
                    }
                }
                procedureIndex++;
            }

            foreach (var index in procedureToRemoveIndexes)
            {
                _procedureMetadata[index] = null;
            }

            _procedureMetadata.RemoveAll(p => p == null);
        }

        protected override string GetStoredProcedureSqlCreate(string procedureName, string schema)
        {
            NpgsqlCommand command = new NpgsqlCommand(@"
            SELECT prosrc
            FROM pg_catalog.pg_proc
                     JOIN pg_catalog.pg_namespace ON (pg_proc.pronamespace = pg_namespace.oid)
                     JOIN pg_catalog.pg_language ON (pg_proc.prolang = pg_language.oid)
            WHERE
                pg_proc.prorettype <> 'pg_catalog.cstring'::pg_catalog.regtype
              AND (pg_proc.proargtypes[0] IS NULL
                OR pg_proc.proargtypes[0] <> 'pg_catalog.cstring'::pg_catalog.regtype)
              AND pg_namespace.nspname = '" + schema + @"'
              AND pg_proc.proname = '"  + procedureName + @"'
              AND pg_catalog.pg_function_is_visible(pg_proc.oid);
            ", _connection);
            using var reader = command.ExecuteReader();
            string procedureText = "";
            while (reader.Read())
            {
                procedureText += reader.GetString(0);
            }
            return procedureText;
        }

        private NpgsqlDbType GetNpgsqlDbType(string pgType)
        {
            return pgType.TrimStart().TrimEnd().ToLower() switch
            {
                "character" => NpgsqlDbType.Char,
                "integer" => NpgsqlDbType.Integer,
                "text" => NpgsqlDbType.Text,
                "boolean" => NpgsqlDbType.Boolean,
                "numeric" => NpgsqlDbType.Numeric,
                "double precision" => NpgsqlDbType.Double,
                "timestamp without time zone" => NpgsqlDbType.Timestamp,
                "timestamp with time zone" => NpgsqlDbType.TimestampTz,
                "bytea" => NpgsqlDbType.Bytea,
                _ => NpgsqlDbType.Unknown
            };
        }
        
        protected override string GetCSharpType(string sqlType)
        {
            sqlType = sqlType.TrimStart().TrimEnd();
            if (sqlType.IndexOf("(", StringComparison.Ordinal) != -1)
            {
                sqlType = sqlType.Substring(0, sqlType.IndexOf("(", StringComparison.Ordinal));
            }
    
            return sqlType.ToLower() switch
            {
                "bigint" => "long",
                "bytea" => "byte[]",
                "boolean" => "bool",
                "char" or "character" or "character varying" or "varchar" or "text" => "string",
                "date" => "DateTime",
                "timestamp" or "timestamp without time zone" => "DateTime",
                "timestamp with time zone" => "DateTimeOffset",
                "numeric" or "decimal" => "decimal",
                "double precision" => "double",
                "integer" or "int" => "int",
                "real" => "float",
                "smallint" => "short",
                "json" or "jsonb" or "xml" => "string",
                "time" or "time without time zone" => "TimeSpan",
                "uuid" => "Guid",
                _ => "unknown",
            };
        }
    }
}
