namespace Querier.Api.Application.DTOs.Requests.Entity
{
    /// <summary>
    /// Data transfer object for deleting an entity through generic CRUD operations
    /// </summary>
    public class EntityCRUDDeleteDto
    {
        /// <summary>
        /// The fully qualified name of the DbContext type on the server
        /// </summary>
        public string ContextTypeName { get; set; }

        /// <summary>
        /// The fully qualified name of the entity type within the context namespace
        /// Must be a type present in the specified database context
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// The primary key value of the entity to delete
        /// Type must match the entity's key type (e.g., int, string, Guid)
        /// </summary>
        public object Key { get; set; }
    }
}