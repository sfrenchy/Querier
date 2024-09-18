using System.Collections.Generic;


namespace Querier.Api.Models.Datatable
{
    /// <summary>
    /// Représente une réponse à Datatable contenant pagination et données
    /// </summary>
    public class ServerSideResponse<T>
    {
        /// <summary>
        /// The draw counter that this object is a response to - from the draw parameter sent as part of the data request. 
        /// </summary>
        public int draw { get; set; }

        /// <summary>
        /// Total records, before filtering (i.e. the total number of records in the database)
        /// </summary>
        public int recordsTotal { get; set; }

        /// <summary>
        /// Total records, after filtering (i.e. the total number of records after filtering has been applied - not just the number of records being returned for this page of data).
        /// </summary>
        public int recordsFiltered { get; set; }

        /// <summary>
        /// The data to be displayed in the table. This is an array of data source objects, one for each row, which will be used by DataTables.
        /// </summary>
        public List<T> data { get; set; }

        /// <summary>
        /// The sums by column
        /// </summary>
        public Dictionary<string, object> sums { get; set; }
    }
}