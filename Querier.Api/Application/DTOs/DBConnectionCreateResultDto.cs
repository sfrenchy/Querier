using System.Collections.Generic;
using Querier.Api.Domain.Common.Enums;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object containing the result of a database connection creation attempt
    /// </summary>
    public class DBConnectionCreateResultDto
    {
        /// <summary>
        /// Indicates whether the connection creation encountered any errors
        /// </summary>
        public bool IsInError { get; set; } = false;

        /// <summary>
        /// Current state of the database connection
        /// </summary>
        public DBConnectionState State { get; set; } = DBConnectionState.None;

        /// <summary>
        /// List of messages describing the connection creation process or any errors encountered
        /// </summary>
        public List<string> Messages { get; set; } = new List<string>();
        
        public int Id { get; set; }
    }
}