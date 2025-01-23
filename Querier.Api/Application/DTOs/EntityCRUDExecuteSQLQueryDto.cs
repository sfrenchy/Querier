namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for executing raw SQL queries through the generic CRUD interface
    /// </summary>
    public class EntityCRUDExecuteSQLQueryDto
    {
        /// <summary>
        /// The fully qualified name of the DbContext type on the server
        /// Determines which database the query will be executed against
        /// </summary>
        public string ContextTypeName { get; set; }

        /// <summary>
        /// The raw SQL query to be executed
        /// Should be carefully validated to prevent SQL injection
        /// </summary>
        public string SqlQuery { get; set; }
    }
}