using System.Collections.Generic;

namespace Querier.Api.Models.Datatable
{
    public class ServerSideColumnRequest
    {
        /// <summary>
        /// Column's data source
        /// </summary>
        public string data { get; set; }
        /// <summary>
        /// Column's name
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// Flag to indicate if this column is searchable
        /// </summary>
        public bool searchable { get; set; }
        /// <summary>
        /// Flag to indicate if this column is orderable
        /// </summary>
        public bool orderable { get; set; }
        /// <summary>
        /// Search value to apply to this specific column.
        /// </summary>
        public ServerSideSearchRequest search { get; set; }
    }
}