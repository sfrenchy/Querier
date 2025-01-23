using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object describing a database table's structure
    /// </summary>
    public class DBConnectionTableDescriptionDto
    {
        /// <summary>
        /// Name of the database table
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Database schema containing the table
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// List of columns in the table with their descriptions
        /// </summary>
        public List<DBConnectionColumnDescriptionDto> Columns { get; set; } = new();
    }
}