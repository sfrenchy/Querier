using Querier.Api.Domain.Entities.QDBConnection;

namespace Querier.Api.Domain.Entities.DBConnection
{
    public class ProcedureOutput
    {
        public string Name { get; set; }

        public string CSName
        {
            get
            {
                return SQLStringTools.NormalizeCSString(Name);
            }
        }

        public int Order { get; set; }
        public bool IsNullable { get; set; }
        public string SQLType { get; set; }

        public string CSType
        {
            get
            {
                string sqlType = SQLType;
                if (SQLType.Contains("("))
                {
                    sqlType = sqlType.Substring(0, sqlType.IndexOf('('));
                }
                return SQLStringTools.SQLTypeToCSType(sqlType, IsNullable);
            }
        }
    }
}