using System.Collections.Generic;

namespace Querier.Api.Application.DTOs.Responses.DBConnection
{
    public class DatabaseSchemaResponse
    {
        public List<TableDescription> Tables { get; set; } = new();
        public List<ViewDescription> Views { get; set; } = new();
        public List<StoredProcedureDescription> StoredProcedures { get; set; } = new();
        public List<UserFunctionDescription> UserFunctions { get; set; } = new();
    }

    public class TableDescription
    {
        public string Name { get; set; }
        public string Schema { get; set; }
        public List<ColumnDescription> Columns { get; set; } = new();
    }

    public class ViewDescription
    {
        public string Name { get; set; }
        public string Schema { get; set; }
        public List<ColumnDescription> Columns { get; set; } = new();
    }

    public class ColumnDescription
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsForeignKey { get; set; }
        public string ForeignKeyTable { get; set; }
        public string ForeignKeyColumn { get; set; }
    }

    public class StoredProcedureDescription
    {
        public string Name { get; set; }
        public string Schema { get; set; }
        public List<ParameterDescription> Parameters { get; set; } = new();
    }

    public class UserFunctionDescription
    {
        public string Name { get; set; }
        public string Schema { get; set; }
        public List<ParameterDescription> Parameters { get; set; } = new();
        public string ReturnType { get; set; }
    }

    public class ParameterDescription
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public string Mode { get; set; }
    }
} 