namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for creating or updating an entity through generic CRUD operations
    /// </summary>
    public class EntityCRUDCreateOrUpdateDto
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
        /// Dynamic object containing the entity's properties and their values
        /// The structure must match the entity type's properties
        /// </summary>
        public dynamic Data { get; set; }
    }
}