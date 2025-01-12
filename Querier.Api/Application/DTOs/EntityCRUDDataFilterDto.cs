namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for filtering entities in generic CRUD operations
    /// Represents a single filter condition that can be applied to any entity property
    /// </summary>
    public class EntityCRUDDataFilterDto
    {
        /// <summary>
        /// The property/column definition to filter on
        /// Contains information about the property name and type
        /// </summary>
        public PropertyDefinitionDto Column { get; set; }

        /// <summary>
        /// The comparison operator to use (e.g., "equals", "contains", "greaterThan")
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// The value to compare against, formatted as a string
        /// Will be converted to the appropriate type based on the Column's type
        /// </summary>
        public string Operand { get; set; }
    }
}
