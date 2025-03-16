using Microsoft.Data.SqlClient;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.Data.Sqlite;

namespace Querier.Api.Domain.Services
{
    public class ProcedureMetadataExtractorSqlite : ProcedureMetadataExtractorBase
    {
        private readonly SqliteConnection _connection;
        public ProcedureMetadataExtractorSqlite(string connectionString, DatabaseModel dbModel)
            : base(dbModel)
        {
            ConnectionString = connectionString;
            _connection = new SqliteConnection(connectionString);
            _connection.Open();
            ExtractMetadata();
        }
        protected override string GetProcedureWithParametersQuery => @"";

        protected override DbConnection Connection => _connection;

        protected override void ExtractProcedureOutputMetadata()
        {
        }

        protected override string GetStoredProcedureSqlCreate(string procedureName)
        {
            return "";
        }

        protected override string GetCSharpType(string sqlType)
        {
            if (sqlType.IndexOf("(") != -1)
            {
                sqlType = sqlType.Substring(0, sqlType.IndexOf("("));
            }
            return sqlType.ToLower() switch
            {
                "bigint" => "long",
                "binary" or "varbinary" => "byte[]",
                "bit" => "bool",
                "char" or "nchar" => "string",
                "date" or "datetime" or "datetime2" or "smalldatetime" => "DateTime",
                "datetimeoffset" => "DateTimeOffset",
                "decimal" or "numeric" or "money" or "smallmoney" => "decimal",
                "float" => "double",
                "image" => "byte[]",
                "int" => "int",
                "real" => "float",
                "text" or "ntext" or "varchar" or "nvarchar" => "string",
                "time" => "TimeSpan",
                "tinyint" => "byte",
                "uniqueidentifier" => "Guid",
                "xml" => "string",
                "smallint" => "short",
                _ => "unknown",
            };
        }
    }
}
