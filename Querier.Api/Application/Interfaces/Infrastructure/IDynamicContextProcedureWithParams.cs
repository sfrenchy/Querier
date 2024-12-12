using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Querier.Api.Application.Interfaces.Infrastructure
{
    /// <summary>
    /// This interface would describe a DatasAsync method to retrieve 
    /// all data from a procedure that returns no results.
    /// </summary>
    public interface IDynamicContextProcedureWithParams
    {
        /// <summary>
        ///  This method retrieves the data from the procedure.
        /// </summary>
        /// <returns>Return a task</returns>
        /// <param name="parameters">argument that contains a key/value dictionary that corresponds to the input parameters of the procedure</param>
        Task ExecuteAsync(Dictionary<string, object> parameters);
    }
}