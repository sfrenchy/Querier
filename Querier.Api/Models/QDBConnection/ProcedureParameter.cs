
namespace Querier.Api.Models.QDBConnection
{
    public class ProcedureParameter
	{
		public string Name { get;set;}
		public string SQLType { get;set; }
		public int Length { get;set; }
		public int Precision { get;set; }
		public int Order { get;set; }
		public bool IsOutput { get;set; }
		public bool IsNullable { get; set; }
        public string SqlParameterType
        {
            get
            {
                return SQLStringTools.SQLTypeToCSSqlParameterType(SQLType);
            }
        }
		public string CSType 
        {
			get
            {
                return SQLStringTools.SQLTypeToCSType(SQLType, IsNullable, Length);
            }
        }

		public string CSName
        {
			get
            {
				return SQLStringTools.NormalizeCSString(Name);
            }
        }
	}
}