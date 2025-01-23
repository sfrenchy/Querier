using System;

namespace Querier.Api.Application.DTOs
{
    public class DataStructureDefinitionDto
    {
        /// <summary>
        /// Name of the data structure
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the data structure
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Type of the data structure (usually "object")
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Source type of the data structure (Entity, StoredProcedure, View, Query)
        /// </summary>
        public DataSourceType SourceType { get; set; }

        /// <summary>
        /// JSON Schema representation of the data structure
        /// </summary>
        public string JsonSchema { get; set; }
    }

    public enum DataSourceType
    {
        Entity = 0,
        StoredProcedure = 1,
        View = 2,
        Query = 3
    }
} 