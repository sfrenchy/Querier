namespace Querier.Api.Models.Datatable
{
    public class ServerSideSearchRequest
    {
        /// <summary>
        /// Search value
        /// </summary>
        public string value { get; set; }

        /// <summary>
        /// true if the global filter should be treated as a regular expression for advanced searching, false otherwise
        /// </summary>
        public bool regex { get; set; }
    }
}