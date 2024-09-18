using System.Collections.Generic;

namespace Querier.Api.Models.Datatable
{
    /// <summary>
    /// Représente un model de filtrage produit par Datatable incluant colonnes, ordre, pagination, filtrage
    /// </summary>
    public class ServerSideRequest
    {
        /// <summary>
        /// Draw counter. This is used by DataTables to ensure that the Ajax returns from server-side processing requests are drawn in sequence by DataTables (Ajax requests are asynchronous and thus can return out of sequence).
        /// </summary>
        public int draw { get; set; }

        /// <summary>
        /// an array defining all columns in the table.
        /// </summary>
        public List<ServerSideColumnRequest> columns { get; set; }

        /// <summary>
        /// an array defining how many columns are being ordered upon - i.e. if the array length is 1, then a single column sort is being performed, otherwise a multi-column sort is being performed.
        /// </summary>
        public List<ServerSideOrder> order { get; set; }

        /// <summary>
        /// Paging first record indicator. This is the start point in the current data set (0 index based - i.e. 0 is the first record).
        /// </summary>
        public int start { get; set; }

        /// <summary>
        /// Number of records that the table can display in the current draw. It is expected that the number of records returned will be equal to this number, unless the server has fewer records to return. Note that this can be -1 to indicate that all records should be returned (although that negates any benefits of server-side processing!)
        /// </summary>
        public int length { get; set; }

        /// <summary>
        /// Global search value. To be applied to all columns which have searchable as true.
        /// </summary>
        public ServerSideSearchRequest search { get; set; }
    }
}