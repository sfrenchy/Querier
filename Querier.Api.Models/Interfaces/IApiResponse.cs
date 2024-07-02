using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Querier.Api.Models.Interfaces
{
    public interface IApiResponse
    {
        /// <summary>
        /// The error code
        /// </summary>
        public int ErrorCode { get; set; }
        /// <summary>
        /// The error message
        /// </summary>
        public string ErrorMessage { get; set; }
        /// <summary>
        /// Exception for the developers
        /// </summary>
        public string Exception { get; set; }
    }
}

