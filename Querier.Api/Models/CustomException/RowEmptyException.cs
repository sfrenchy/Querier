using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Querier.Api.Models.CustomException
{
    public class RowEmptyException : Exception
    {
        public RowEmptyException()
        {
        }

        public RowEmptyException(string message)
            : base(message)
        {
        }

        public RowEmptyException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
