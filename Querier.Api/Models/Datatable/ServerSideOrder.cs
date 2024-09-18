namespace Querier.Api.Models.Datatable
{
    public class ServerSideOrder
    {
        /// <summary>
        /// Column to which ordering should be applied. This is an index reference to the columns array of information.
        /// </summary>
        public int column { get; set; }

        /// <summary>
        /// Ordering direction for this column. It will be "asc" or "desc" to indicate ascending ordering or descending ordering, respectively.
        /// </summary>
        public string dir { get; set; }
    }
}