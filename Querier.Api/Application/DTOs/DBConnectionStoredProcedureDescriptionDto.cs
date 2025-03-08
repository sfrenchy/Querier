using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object containing the description of a database stored procedure
    /// </summary>
    public class DbConnectionStoredProcedureDescriptionDto
    {
        /// <summary>
        /// Name of the stored procedure
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Schema containing the stored procedure
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// List of parameters accepted by the stored procedure
        /// </summary>
        public List<DBConnectionParameterDescriptionDto> Parameters { get; set; } = new();

        public List<DBConnectionColumnDescriptionDto> OutputColumns { get; set; } = new();
    }
}