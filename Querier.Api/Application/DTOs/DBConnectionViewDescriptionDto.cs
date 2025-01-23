using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object containing the description of a database view
    /// </summary>
    public class DBConnectionViewDescriptionDto
    {
        /// <summary>
        /// Name of the database view
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Schema containing the view
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// List of columns in the view with their descriptions
        /// </summary>
        public List<DBConnectionColumnDescriptionDto> Columns { get; set; } = new();
    }
}