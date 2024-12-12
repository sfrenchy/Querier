using System.Collections.Generic;
using Querier.Api.Models.Common;

namespace Querier.Api.Models.Requests
{
    public class CRUDReadRequest
    {
        /// <summary>
        /// The server side type of the context
        /// </summary>
        public string? ContextTypeName { get; set; }

        /// <summary>
        /// The server side type of the entity
        /// </summary>
        public string? EntityType { get; set; }

        public List<DataFilter> Filters { get; set; }
    }

    public class CRUDCreateOrUpdateRequest
    {
        /// <summary>
        /// The server side type of the context
        /// </summary>
        public string? ContextTypeName { get; set; }

        /// <summary>
        /// The server side type of the entity in context namespace and present in the according database context
        /// </summary>
        public string? EntityType { get; set; }

        /// <summary>
        /// The properties of the entity
        /// </summary>
        public dynamic? Data { get; set; }
    }

    public class CRUDDeleteRequest
    {
        /// <summary>
        /// The server side type of the context
        /// </summary>
        public string? ContextTypeName { get; set; }

        /// <summary>
        /// The server side type of the entity
        /// </summary>
        public string? EntityType { get; set; }

        /// <summary>
        /// The entity's key to delete
        /// </summary>
        public object? Key { get; set; }
    }

    public class CRUDExecuteSQLQueryRequest
    {
        /// <summary>
        /// The server side type of the context
        /// </summary>
        public string? ContextTypeName { get; set; }

        /// <summary>
        /// The query to be executed
        /// </summary>
        public string SqlQuery { get; set; }
    }

    public class CRUDReadSqlQueryRequest
    {
        /// <summary>
        /// The server side type of the context
        /// </summary>
        public string? ContextTypeName { get; set; }

        /// <summary>
        /// The SqlQuery
        /// </summary>
        public string? SqlQuery { get; set; }

        public List<DataFilter> Filters { get; set; }
    }

    /// <summary>
    /// THis is filter that can be applied to any entity of a db model
    /// </summary>
    public class DataFilter
    {
        public PropertyDefinition Column { get; set;}
        public string Operator { get; set; }
        public string Operand { get; set; }
    }
}
